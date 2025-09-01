using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RdpRelay.Agent.Win.Models;

namespace RdpRelay.Agent.Win.Services;

public interface IRdpConnectionManager
{
    Task<string> CreateConnectionAsync(string sessionId, CancellationToken cancellationToken);
    Task CloseConnectionAsync(string connectionId);
    Task<bool> IsConnectionActiveAsync(string connectionId);
    int GetActiveConnectionsCount();
}

public class RdpConnectionManager : IRdpConnectionManager
{
    private readonly ILogger<RdpConnectionManager> _logger;
    private readonly AgentOptions _options;
    private readonly ConcurrentDictionary<string, RdpConnection> _connections;
    private readonly SemaphoreSlim _connectionSemaphore;

    public RdpConnectionManager(
        ILogger<RdpConnectionManager> logger,
        IOptions<AgentOptions> options)
    {
        _logger = logger;
        _options = options.Value;
        _connections = new ConcurrentDictionary<string, RdpConnection>();
        _connectionSemaphore = new SemaphoreSlim(_options.MaxRdpConnections, _options.MaxRdpConnections);
    }

    public async Task<string> CreateConnectionAsync(string sessionId, CancellationToken cancellationToken)
    {
        if (!await _connectionSemaphore.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken))
        {
            throw new InvalidOperationException($"Maximum RDP connections ({_options.MaxRdpConnections}) exceeded");
        }

        try
        {
            var connectionId = Guid.NewGuid().ToString();
            var localPort = await GetAvailablePortAsync();

            var connection = new RdpConnection
            {
                ConnectionId = connectionId,
                SessionId = sessionId,
                LocalPort = localPort,
                Status = RdpConnectionStatus.Connecting,
                CreatedAt = DateTime.UtcNow
            };

            // Start TCP listener for RDP traffic
            var listener = new TcpListener(IPAddress.Loopback, localPort);
            listener.Start();
            connection.TcpListener = listener;

            _connections.TryAdd(connectionId, connection);
            _logger.LogInformation("Created RDP connection {ConnectionId} for session {SessionId} on port {Port}",
                connectionId, sessionId, localPort);

            // Start accepting connections in background
            _ = Task.Run(() => AcceptRdpConnectionsAsync(connection, cancellationToken), cancellationToken);

            return connectionId;
        }
        catch
        {
            _connectionSemaphore.Release();
            throw;
        }
    }

    public async Task CloseConnectionAsync(string connectionId)
    {
        if (_connections.TryRemove(connectionId, out var connection))
        {
            try
            {
                connection.Status = RdpConnectionStatus.Disconnected;
                connection.TcpListener?.Stop();
                
                if (connection.RdpClient != null)
                {
                    connection.RdpClient.Close();
                    connection.RdpClient.Dispose();
                }

                _logger.LogInformation("Closed RDP connection {ConnectionId}", connectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing RDP connection {ConnectionId}", connectionId);
            }
            finally
            {
                _connectionSemaphore.Release();
            }
        }

        await Task.CompletedTask;
    }

    public async Task<bool> IsConnectionActiveAsync(string connectionId)
    {
        await Task.CompletedTask;
        return _connections.TryGetValue(connectionId, out var connection) 
               && connection.Status == RdpConnectionStatus.Connected;
    }

    public int GetActiveConnectionsCount()
    {
        return _connections.Count(c => c.Value.Status == RdpConnectionStatus.Connected);
    }

    private async Task<int> GetAvailablePortAsync()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        try
        {
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            return port;
        }
        finally
        {
            listener.Stop();
        }
    }

    private async Task AcceptRdpConnectionsAsync(RdpConnection connection, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && 
                   connection.Status != RdpConnectionStatus.Disconnected)
            {
                try
                {
                    var tcpClient = await connection.TcpListener!.AcceptTcpClientAsync();
                    connection.RdpClient = tcpClient;
                    connection.Status = RdpConnectionStatus.Connected;

                    _logger.LogInformation("RDP client connected to connection {ConnectionId}", 
                        connection.ConnectionId);

                    // In a real implementation, this would handle RDP protocol traffic
                    // and relay it through the WebSocket connection to the relay server
                    _ = Task.Run(() => HandleRdpClientAsync(connection, cancellationToken), cancellationToken);
                }
                catch (ObjectDisposedException)
                {
                    // Listener was stopped
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error accepting RDP connection for {ConnectionId}", 
                        connection.ConnectionId);
                    await Task.Delay(1000, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("RDP connection acceptor cancelled for {ConnectionId}", connection.ConnectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in RDP connection acceptor for {ConnectionId}", 
                connection.ConnectionId);
        }
    }

    private async Task HandleRdpClientAsync(RdpConnection connection, CancellationToken cancellationToken)
    {
        try
        {
            using var stream = connection.RdpClient!.GetStream();
            var buffer = new byte[4096];

            while (!cancellationToken.IsCancellationRequested && 
                   connection.RdpClient.Connected)
            {
                try
                {
                    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                    if (bytesRead == 0)
                    {
                        break; // Client disconnected
                    }

                    // In a real implementation, this data would be forwarded
                    // to the relay server via WebSocket for transmission to the RDP client
                    _logger.LogDebug("Received {Bytes} bytes from RDP client for connection {ConnectionId}",
                        bytesRead, connection.ConnectionId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error reading from RDP client for connection {ConnectionId}",
                        connection.ConnectionId);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling RDP client for connection {ConnectionId}",
                connection.ConnectionId);
        }
        finally
        {
            connection.Status = RdpConnectionStatus.Disconnected;
            _logger.LogInformation("RDP client disconnected from connection {ConnectionId}",
                connection.ConnectionId);
        }
    }
}
