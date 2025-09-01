using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RdpRelay.Relay.Models;

namespace RdpRelay.Relay.Services;

public interface ITcpRelayService
{
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}

public class TcpRelayService : ITcpRelayService
{
    private readonly ILogger<TcpRelayService> _logger;
    private readonly RelayOptions _options;
    private readonly IAgentRegistry _agentRegistry;
    private readonly ISessionBroker _sessionBroker;
    private TcpListener? _tcpListener;
    private CancellationTokenSource? _cancellationTokenSource;

    public TcpRelayService(
        ILogger<TcpRelayService> logger,
        IOptions<RelayOptions> options,
        IAgentRegistry agentRegistry,
        ISessionBroker sessionBroker)
    {
        _logger = logger;
        _options = options.Value;
        _agentRegistry = agentRegistry;
        _sessionBroker = sessionBroker;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var bindParts = _options.Bind.Split(':');
            var bindAddress = bindParts[0] == "" ? IPAddress.Any : IPAddress.Parse(bindParts[0]);
            var bindPort = int.Parse(bindParts[1]);

            _tcpListener = new TcpListener(bindAddress, bindPort);
            _tcpListener.Start();

            _cancellationTokenSource = new CancellationTokenSource();
            _ = Task.Run(() => AcceptClientsAsync(_cancellationTokenSource.Token), cancellationToken);

            _logger.LogInformation("TCP Relay Service started on {Bind}", _options.Bind);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start TCP Relay Service");
            throw;
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource?.Cancel();
        _tcpListener?.Stop();
        _logger.LogInformation("TCP Relay Service stopped");
        return Task.CompletedTask;
    }

    private async Task AcceptClientsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _tcpListener != null)
        {
            try
            {
                var client = await _tcpListener.AcceptTcpClientAsync();
                _ = Task.Run(() => HandleClientAsync(client, cancellationToken), cancellationToken);
            }
            catch (ObjectDisposedException)
            {
                // TcpListener was stopped
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting TCP client");
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        var clientEndpoint = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
        var sessionId = $"TCP-{Guid.NewGuid():N}";

        try
        {
            _logger.LogInformation("New TCP connection from {ClientEndpoint} (Session: {SessionId})", clientEndpoint, sessionId);

            client.NoDelay = true;
            client.ReceiveTimeout = 30000;
            client.SendTimeout = 30000;

            var stream = client.GetStream();

            // Read initial bytes to detect RDP X.224 Connection Request
            var buffer = new byte[128];
            var bytesRead = await stream.ReadAsync(buffer, cancellationToken);

            if (bytesRead < 4)
            {
                _logger.LogWarning("Insufficient data from client {ClientEndpoint}", clientEndpoint);
                return;
            }

            // Detect RDP X.224 Connection Request (starts with 0x03 0x00 <length>)
            if (buffer[0] != 0x03 || buffer[1] != 0x00)
            {
                _logger.LogWarning("Non-RDP connection from {ClientEndpoint}, rejecting", clientEndpoint);
                return;
            }

            _logger.LogInformation("Detected RDP X.224 Connection Request from {ClientEndpoint}", clientEndpoint);

            // Extract connect code from RDP connection data if present
            string? connectCode = ExtractConnectCodeFromRdpData(buffer, bytesRead);
            
            // Try to find a session using the connect code
            AgentConnection? targetAgent = null;
            if (!string.IsNullOrEmpty(connectCode))
            {
                _logger.LogInformation("Connect code {ConnectCode} detected for session {SessionId}", connectCode, sessionId);
                
                // In a full implementation, we would look up the session by connect code
                // For now, we'll use the first available agent but log the connect code
                var agents = await _agentRegistry.GetAgentsAsync();
                targetAgent = agents.FirstOrDefault(a => a.Status == AgentStatus.Online);
            }
            else
            {
                _logger.LogInformation("No connect code detected, using simple agent selection");
                var agents = await _agentRegistry.GetAgentsAsync();
                targetAgent = agents.FirstOrDefault(a => a.Status == AgentStatus.Online);
            }

            if (targetAgent == null)
            {
                _logger.LogWarning("No available agents for session {SessionId}", sessionId);
                return;
            }

            _logger.LogInformation("Pairing session {SessionId} with agent {AgentId}", sessionId, targetAgent.AgentId);

            // Start data channel with agent
            await StartAgentDataChannelAsync(availableAgent, sessionId);

            // Relay data between client and agent
            await RelayDataAsync(stream, availableAgent.WebSocket, buffer, bytesRead, sessionId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling TCP client {ClientEndpoint} (Session: {SessionId})", clientEndpoint, sessionId);
        }
        finally
        {
            try
            {
                client.Close();
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    private async Task StartAgentDataChannelAsync(AgentConnection agent, string sessionId)
    {
        var startMessage = new AgentMessage
        {
            Type = "start",
            SessionId = sessionId,
            AgentId = agent.AgentId
        };

        var json = JsonSerializer.Serialize(startMessage);
        var bytes = Encoding.UTF8.GetBytes(json);
        
        await agent.WebSocket.SendAsync(
            new ArraySegment<byte>(bytes), 
            WebSocketMessageType.Text, 
            true, 
            CancellationToken.None);

        agent.Status = AgentStatus.InSession;
    }

    private async Task RelayDataAsync(
        NetworkStream clientStream,
        WebSocket agentWebSocket,
        byte[] initialBuffer,
        int initialBytesRead,
        string sessionId,
        CancellationToken cancellationToken)
    {
        var bytesUp = 0L;
        var bytesDown = 0L;

        try
        {
            // Send initial RDP bytes to agent
            if (initialBytesRead > 0)
            {
                await agentWebSocket.SendAsync(
                    new ArraySegment<byte>(initialBuffer, 0, initialBytesRead),
                    WebSocketMessageType.Binary,
                    true,
                    cancellationToken);
                bytesUp += initialBytesRead;
            }

            // Start bidirectional relay
            var clientToAgent = RelayClientToAgentAsync(clientStream, agentWebSocket, cancellationToken);
            var agentToClient = RelayAgentToClientAsync(agentWebSocket, clientStream, cancellationToken);

            // Wait for either direction to complete
            var completedTask = await Task.WhenAny(clientToAgent, agentToClient);
            
            bytesUp += await clientToAgent;
            bytesDown += await agentToClient;

            _logger.LogInformation("Session {SessionId} completed. Bytes: Up={BytesUp}, Down={BytesDown}", 
                sessionId, bytesUp, bytesDown);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error relaying data for session {SessionId}", sessionId);
        }
        finally
        {
            await _sessionBroker.CompleteSessionAsync(sessionId, bytesUp, bytesDown, "TCP relay ended");
        }
    }

    private async Task<long> RelayClientToAgentAsync(
        NetworkStream clientStream,
        WebSocket agentWebSocket,
        CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(64 * 1024);
        var totalBytes = 0L;

        try
        {
            while (!cancellationToken.IsCancellationRequested && 
                   agentWebSocket.State == WebSocketState.Open)
            {
                var bytesRead = await clientStream.ReadAsync(buffer, cancellationToken);
                if (bytesRead == 0) break;

                await agentWebSocket.SendAsync(
                    new ArraySegment<byte>(buffer, 0, bytesRead),
                    WebSocketMessageType.Binary,
                    true,
                    cancellationToken);

                totalBytes += bytesRead;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogDebug(ex, "Client to agent relay ended");
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        return totalBytes;
    }

    private async Task<long> RelayAgentToClientAsync(
        WebSocket agentWebSocket,
        NetworkStream clientStream,
        CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(64 * 1024);
        var totalBytes = 0L;

        try
        {
            while (!cancellationToken.IsCancellationRequested && 
                   agentWebSocket.State == WebSocketState.Open)
            {
                var result = await agentWebSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close) break;
                if (result.MessageType != WebSocketMessageType.Binary) continue;

                await clientStream.WriteAsync(buffer.AsMemory(0, result.Count), cancellationToken);
                totalBytes += result.Count;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogDebug(ex, "Agent to client relay ended");
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        return totalBytes;
    }
}
