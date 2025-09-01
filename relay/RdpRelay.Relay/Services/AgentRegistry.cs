using RdpRelay.Relay.Models;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace RdpRelay.Relay.Services;

public interface IAgentRegistry
{
    Task HandleAgentConnectionAsync(AgentConnection connection);
    Task<AgentConnection?> GetAgentAsync(string agentId);
    Task<int> GetActiveAgentCountAsync();
    Task<IEnumerable<AgentConnection>> GetAgentsAsync(string? tenantId = null);
    Task DisconnectAgentAsync(string agentId);
}

public class AgentRegistry : IAgentRegistry
{
    private readonly ConcurrentDictionary<string, AgentConnection> _agents = new();
    private readonly ILogger<AgentRegistry> _logger;
    private readonly IServiceProvider _serviceProvider;

    public AgentRegistry(ILogger<AgentRegistry> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task HandleAgentConnectionAsync(AgentConnection connection)
    {
        try
        {
            _logger.LogInformation("New agent connection from {RemoteEndpoint}", connection.WebSocket.State);

            // Wait for Hello message
            var helloMessage = await ReceiveMessageAsync(connection.WebSocket, connection.CancellationToken);
            if (helloMessage?.Type != "hello")
            {
                _logger.LogWarning("Expected hello message, got {MessageType}", helloMessage?.Type);
                await connection.WebSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Expected hello message", connection.CancellationToken);
                return;
            }

            // Validate and register agent
            if (string.IsNullOrEmpty(helloMessage.AgentId) || string.IsNullOrEmpty(helloMessage.TenantId))
            {
                _logger.LogWarning("Invalid hello message: missing agentId or tenantId");
                await connection.WebSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Invalid hello message", connection.CancellationToken);
                return;
            }

            connection.AgentId = helloMessage.AgentId;
            connection.TenantId = helloMessage.TenantId;
            connection.GroupId = helloMessage.GroupId;
            connection.Hostname = helloMessage.Hostname;
            connection.OsVersion = helloMessage.OsVersion;
            connection.Status = AgentStatus.Online;

            // Add to registry
            _agents.AddOrUpdate(connection.AgentId, connection, (key, oldValue) => connection);

            _logger.LogInformation("Agent {AgentId} from tenant {TenantId} connected", connection.AgentId, connection.TenantId);

            // Send acknowledgment
            await SendMessageAsync(connection.WebSocket, new AgentMessage
            {
                Type = "hello_ack",
                AgentId = connection.AgentId
            }, connection.CancellationToken);

            // Handle messages
            await HandleAgentMessagesAsync(connection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling agent connection");
        }
        finally
        {
            if (connection.AgentId != null)
            {
                _agents.TryRemove(connection.AgentId, out _);
                _logger.LogInformation("Agent {AgentId} disconnected", connection.AgentId);
            }
        }
    }

    private async Task HandleAgentMessagesAsync(AgentConnection connection)
    {
        while (connection.WebSocket.State == WebSocketState.Open)
        {
            try
            {
                var message = await ReceiveMessageAsync(connection.WebSocket, connection.CancellationToken);
                if (message == null) break;

                switch (message.Type)
                {
                    case "hb": // heartbeat
                        connection.LastHeartbeatAt = DateTime.UtcNow;
                        await SendMessageAsync(connection.WebSocket, new AgentMessage
                        {
                            Type = "hb_ack",
                            AgentId = connection.AgentId
                        }, connection.CancellationToken);
                        break;

                    case "start_ack":
                        // Agent acknowledged session start
                        _logger.LogInformation("Agent {AgentId} acknowledged session {SessionId}", connection.AgentId, message.SessionId);
                        break;

                    case "session_ended":
                        // Agent reported session end
                        _logger.LogInformation("Agent {AgentId} reported session {SessionId} ended", connection.AgentId, message.SessionId);
                        connection.Status = AgentStatus.Online;
                        break;

                    case "error":
                        _logger.LogWarning("Agent {AgentId} reported error: {Error}", connection.AgentId, message.Error);
                        break;

                    default:
                        _logger.LogWarning("Unknown message type {MessageType} from agent {AgentId}", message.Type, connection.AgentId);
                        break;
                }
            }
            catch (WebSocketException ex) when (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
            {
                _logger.LogInformation("Agent {AgentId} connection closed", connection.AgentId);
                break;
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling agent message from {AgentId}", connection.AgentId);
                break;
            }
        }
    }

    private async Task<AgentMessage?> ReceiveMessageAsync(WebSocket webSocket, CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
        
        if (result.MessageType == WebSocketMessageType.Close)
            return null;

        var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
        return JsonSerializer.Deserialize<AgentMessage>(json);
    }

    private async Task SendMessageAsync(WebSocket webSocket, AgentMessage message, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(message);
        var bytes = Encoding.UTF8.GetBytes(json);
        await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);
    }

    public Task<AgentConnection?> GetAgentAsync(string agentId)
    {
        _agents.TryGetValue(agentId, out var agent);
        return Task.FromResult(agent);
    }

    public Task<int> GetActiveAgentCountAsync()
    {
        return Task.FromResult(_agents.Count(a => a.Value.Status == AgentStatus.Online));
    }

    public Task<IEnumerable<AgentConnection>> GetAgentsAsync(string? tenantId = null)
    {
        var agents = _agents.Values.AsEnumerable();
        if (tenantId != null)
            agents = agents.Where(a => a.TenantId == tenantId);
        
        return Task.FromResult(agents);
    }

    public async Task DisconnectAgentAsync(string agentId)
    {
        if (_agents.TryGetValue(agentId, out var agent))
        {
            try
            {
                await agent.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Requested disconnect", CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting agent {AgentId}", agentId);
            }
        }
    }
}
