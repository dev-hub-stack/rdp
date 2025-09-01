using System.Net.WebSockets;
using System.Text.Json.Serialization;

namespace RdpRelay.Relay.Models;

public class AgentConnection
{
    public WebSocket WebSocket { get; }
    public CancellationToken CancellationToken { get; }
    public string? AgentId { get; set; }
    public string? TenantId { get; set; }
    public string? GroupId { get; set; }
    public string? Hostname { get; set; }
    public string? OsVersion { get; set; }
    public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastHeartbeatAt { get; set; } = DateTime.UtcNow;
    public AgentStatus Status { get; set; } = AgentStatus.Connecting;

    public AgentConnection(WebSocket webSocket, CancellationToken cancellationToken)
    {
        WebSocket = webSocket;
        CancellationToken = cancellationToken;
    }
}

public enum AgentStatus
{
    Connecting,
    Online,
    Offline,
    InSession
}

public class AgentMessage
{
    [JsonPropertyName("t")]
    public string Type { get; set; } = "";

    [JsonPropertyName("agentId")]
    public string? AgentId { get; set; }

    [JsonPropertyName("tenantId")]
    public string? TenantId { get; set; }

    [JsonPropertyName("groupId")]
    public string? GroupId { get; set; }

    [JsonPropertyName("hostname")]
    public string? Hostname { get; set; }

    [JsonPropertyName("os")]
    public string? OsVersion { get; set; }

    [JsonPropertyName("ts")]
    public long? Timestamp { get; set; }

    [JsonPropertyName("sid")]
    public string? SessionId { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

public class SessionRequest
{
    public string SessionId { get; set; } = "";
    public string AgentId { get; set; } = "";
    public string TenantId { get; set; } = "";
    public string OperatorUserId { get; set; } = "";
    public string? ConnectCode { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public SessionStatus Status { get; set; } = SessionStatus.Requested;
}

public enum SessionStatus
{
    Requested,
    Paired,
    Active,
    Ended,
    Failed,
    Expired
}
