using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RdpRelay.Agent.Win.Models;

namespace RdpRelay.Agent.Win.Services;

public interface IRelayWebSocketClient : IDisposable
{
    Task ConnectAsync(CancellationToken cancellationToken);
    Task SendMessageAsync(WebSocketMessage message, CancellationToken cancellationToken);
    event Func<WebSocketMessage, Task>? OnMessageReceived;
    bool IsConnected { get; }
}

public class RelayWebSocketClient : IRelayWebSocketClient
{
    private readonly ILogger<RelayWebSocketClient> _logger;
    private readonly AgentOptions _options;
    private ClientWebSocket? _webSocket;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _receiveTask;

    public event Func<WebSocketMessage, Task>? OnMessageReceived;
    public bool IsConnected => _webSocket?.State == WebSocketState.Open;

    public RelayWebSocketClient(
        ILogger<RelayWebSocketClient> logger,
        IOptions<AgentOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        try
        {
            _webSocket = new ClientWebSocket();
            _webSocket.Options.SetRequestHeader("X-Provisioning-Token", _options.ProvisioningToken);

            var uri = new Uri(_options.RelayUrl.Replace("http", "ws"));
            _logger.LogInformation("Connecting to relay server at {Uri}", uri);

            await _webSocket.ConnectAsync(uri, cancellationToken);
            _logger.LogInformation("Connected to relay server");

            _cancellationTokenSource = new CancellationTokenSource();
            _receiveTask = ReceiveLoopAsync(_cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to relay server");
            throw;
        }
    }

    public async Task SendMessageAsync(WebSocketMessage message, CancellationToken cancellationToken)
    {
        if (_webSocket?.State != WebSocketState.Open)
        {
            throw new InvalidOperationException("WebSocket is not connected");
        }

        try
        {
            var json = JsonSerializer.Serialize(message);
            var bytes = Encoding.UTF8.GetBytes(json);
            await _webSocket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message: {MessageType}", message.Type);
            throw;
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        
        try
        {
            while (!cancellationToken.IsCancellationRequested && _webSocket?.State == WebSocketState.Open)
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var message = JsonSerializer.Deserialize<WebSocketMessage>(json);
                    
                    if (message != null && OnMessageReceived != null)
                    {
                        try
                        {
                            await OnMessageReceived(message);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error handling received message: {MessageType}", message.Type);
                        }
                    }
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.LogInformation("WebSocket connection closed by server");
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("WebSocket receive loop cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in WebSocket receive loop");
        }
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Cancel();
        _receiveTask?.Wait(TimeSpan.FromSeconds(5));
        _webSocket?.Dispose();
        _cancellationTokenSource?.Dispose();
    }
}
