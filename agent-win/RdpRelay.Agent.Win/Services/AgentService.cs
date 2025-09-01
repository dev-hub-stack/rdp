using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RdpRelay.Agent.Win.Models;
using RdpRelay.Agent.Win.Services;
using System.Text.Json;

namespace RdpRelay.Agent.Win.Services;

public class AgentService : BackgroundService
{
    private readonly ILogger<AgentService> _logger;
    private readonly AgentOptions _options;
    private readonly IRelayWebSocketClient _webSocketClient;
    private readonly ISystemInfoService _systemInfoService;
    private readonly IRdpConnectionManager _rdpConnectionManager;
    private Timer? _heartbeatTimer;
    private string? _agentId;

    public AgentService(
        ILogger<AgentService> logger,
        IOptions<AgentOptions> options,
        IRelayWebSocketClient webSocketClient,
        ISystemInfoService systemInfoService,
        IRdpConnectionManager rdpConnectionManager)
    {
        _logger = logger;
        _options = options.Value;
        _webSocketClient = webSocketClient;
        _systemInfoService = systemInfoService;
        _rdpConnectionManager = rdpConnectionManager;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Agent service starting");

        // Set up message handler
        _webSocketClient.OnMessageReceived += HandleWebSocketMessageAsync;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Connect to relay server
                await ConnectToRelayAsync(stoppingToken);

                // Register agent
                await RegisterAgentAsync(stoppingToken);

                // Start heartbeat timer
                StartHeartbeatTimer();

                // Keep connection alive
                while (_webSocketClient.IsConnected && !stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Agent service stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in agent service, reconnecting in 30 seconds");
                await Task.Delay(30000, stoppingToken);
            }
            finally
            {
                StopHeartbeatTimer();
            }
        }

        _logger.LogInformation("Agent service stopped");
    }

    private async Task ConnectToRelayAsync(CancellationToken cancellationToken)
    {
        var maxRetries = 5;
        var retryDelay = TimeSpan.FromSeconds(5);

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                await _webSocketClient.ConnectAsync(cancellationToken);
                _logger.LogInformation("Successfully connected to relay server");
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to connect to relay server (attempt {Attempt}/{MaxRetries})",
                    i + 1, maxRetries);
                
                if (i < maxRetries - 1)
                {
                    await Task.Delay(retryDelay, cancellationToken);
                    retryDelay = TimeSpan.FromMilliseconds(retryDelay.TotalMilliseconds * 1.5); // Exponential backoff
                }
            }
        }

        throw new InvalidOperationException("Failed to connect to relay server after multiple attempts");
    }

    private async Task RegisterAgentAsync(CancellationToken cancellationToken)
    {
        try
        {
            var systemInfo = await _systemInfoService.GetSystemInfoAsync();
            
            var registrationMessage = new WebSocketMessage
            {
                Type = "agent_register",
                Data = JsonSerializer.SerializeToElement(new AgentRegistration
                {
                    SystemInfo = systemInfo,
                    Version = "1.0.0",
                    Capabilities = new List<string> { "rdp", "system_info" },
                    MaxConnections = _options.MaxRdpConnections
                })
            };

            await _webSocketClient.SendMessageAsync(registrationMessage, cancellationToken);
            _logger.LogInformation("Agent registration request sent");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register agent");
            throw;
        }
    }

    private void StartHeartbeatTimer()
    {
        _heartbeatTimer = new Timer(
            SendHeartbeatAsync,
            null,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(_options.HeartbeatIntervalSeconds));
    }

    private void StopHeartbeatTimer()
    {
        _heartbeatTimer?.Dispose();
        _heartbeatTimer = null;
    }

    private async void SendHeartbeatAsync(object? state)
    {
        try
        {
            if (!_webSocketClient.IsConnected)
                return;

            var activeConnections = _rdpConnectionManager.GetActiveConnectionsCount();
            var systemInfo = await _systemInfoService.GetSystemInfoAsync();

            var heartbeatMessage = new WebSocketMessage
            {
                Type = "agent_heartbeat",
                Data = JsonSerializer.SerializeToElement(new AgentHeartbeat
                {
                    AgentId = _agentId,
                    SystemInfo = systemInfo,
                    ActiveConnections = activeConnections,
                    Timestamp = DateTime.UtcNow
                })
            };

            await _webSocketClient.SendMessageAsync(heartbeatMessage, CancellationToken.None);
            _logger.LogDebug("Heartbeat sent: {ActiveConnections} active connections", activeConnections);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send heartbeat");
        }
    }

    private async Task HandleWebSocketMessageAsync(WebSocketMessage message)
    {
        try
        {
            _logger.LogDebug("Received message: {MessageType}", message.Type);

            switch (message.Type)
            {
                case "agent_registered":
                    await HandleAgentRegisteredAsync(message);
                    break;

                case "session_start":
                    await HandleSessionStartAsync(message);
                    break;

                case "session_end":
                    await HandleSessionEndAsync(message);
                    break;

                case "rdp_data":
                    await HandleRdpDataAsync(message);
                    break;

                default:
                    _logger.LogWarning("Unknown message type: {MessageType}", message.Type);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling WebSocket message: {MessageType}", message.Type);
        }
    }

    private async Task HandleAgentRegisteredAsync(WebSocketMessage message)
    {
        try
        {
            var registration = JsonSerializer.Deserialize<AgentRegisteredResponse>(message.Data);
            if (registration != null)
            {
                _agentId = registration.AgentId;
                _logger.LogInformation("Agent registered successfully with ID: {AgentId}", _agentId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle agent registration response");
        }

        await Task.CompletedTask;
    }

    private async Task HandleSessionStartAsync(WebSocketMessage message)
    {
        try
        {
            var sessionStart = JsonSerializer.Deserialize<SessionStartRequest>(message.Data);
            if (sessionStart != null)
            {
                _logger.LogInformation("Starting RDP session: {SessionId}", sessionStart.SessionId);
                
                var connectionId = await _rdpConnectionManager.CreateConnectionAsync(
                    sessionStart.SessionId, CancellationToken.None);

                var response = new WebSocketMessage
                {
                    Type = "session_started",
                    Data = JsonSerializer.SerializeToElement(new SessionStartedResponse
                    {
                        SessionId = sessionStart.SessionId,
                        ConnectionId = connectionId,
                        Status = "started"
                    })
                };

                await _webSocketClient.SendMessageAsync(response, CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start RDP session");
            
            // Send error response
            var sessionStart = JsonSerializer.Deserialize<SessionStartRequest>(message.Data);
            if (sessionStart != null)
            {
                var errorResponse = new WebSocketMessage
                {
                    Type = "session_error",
                    Data = JsonSerializer.SerializeToElement(new SessionErrorResponse
                    {
                        SessionId = sessionStart.SessionId,
                        Error = ex.Message
                    })
                };

                await _webSocketClient.SendMessageAsync(errorResponse, CancellationToken.None);
            }
        }
    }

    private async Task HandleSessionEndAsync(WebSocketMessage message)
    {
        try
        {
            var sessionEnd = JsonSerializer.Deserialize<SessionEndRequest>(message.Data);
            if (sessionEnd != null)
            {
                _logger.LogInformation("Ending RDP session: {SessionId}", sessionEnd.SessionId);
                await _rdpConnectionManager.CloseConnectionAsync(sessionEnd.ConnectionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to end RDP session");
        }
    }

    private async Task HandleRdpDataAsync(WebSocketMessage message)
    {
        try
        {
            // In a real implementation, this would forward RDP data 
            // to the local RDP connection
            _logger.LogDebug("Received RDP data message");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle RDP data");
        }

        await Task.CompletedTask;
    }

    public override void Dispose()
    {
        StopHeartbeatTimer();
        _webSocketClient?.Dispose();
        base.Dispose();
    }
}
