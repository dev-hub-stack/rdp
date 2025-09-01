namespace RdpRelay.Agent.Win.Models;

public class AgentOptions
{
    public string RelayUrl { get; set; } = string.Empty;
    public string ProvisioningToken { get; set; } = string.Empty;
    public int HeartbeatIntervalSeconds { get; set; } = 30;
    public int MaxRdpConnections { get; set; } = 5;
    public string LogLevel { get; set; } = "Information";
}

public class AgentInfo
{
    public string Id { get; set; } = "";
    public string MachineId { get; set; } = "";
    public string MachineName { get; set; } = "";
    public string IpAddress { get; set; } = "";
    public string OsVersion { get; set; } = "";
    public string Version { get; set; } = "";
    public int RdpPort { get; set; } = 3389;
    public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
}

public class SessionInfo
{
    public string SessionId { get; set; } = "";
    public string ConnectCode { get; set; } = "";
    public string RelayEndpoint { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
}

public enum AgentStatus
{
    Disconnected,
    Connecting, 
    Connected,
    InSession,
    Error
}

public class AgentMessage
{
    public string Type { get; set; } = "";
    public string Data { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class HeartbeatMessage
{
    public string Status { get; set; } = "online";
    public string Version { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class SessionMessage
{
    public string SessionId { get; set; } = "";
    public string ConnectCode { get; set; } = "";
    public string Action { get; set; } = ""; // start, stop, status
    public string Status { get; set; } = "";
    public string? Error { get; set; }
}

public class RdpConnectionInfo
{
    public string ServerHost { get; set; } = "127.0.0.1";
    public int ServerPort { get; set; } = 3389;
    public string SessionId { get; set; } = "";
    public bool IsActive { get; set; }
    public DateTime StartedAt { get; set; }
}

public class WebSocketMessage
{
    public string Type { get; set; } = "";
    public System.Text.Json.JsonElement Data { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class SystemInfo
{
    public string ComputerName { get; set; } = "";
    public string OperatingSystem { get; set; } = "";
    public int ProcessorCount { get; set; }
    public long TotalMemoryMB { get; set; }
    public string Architecture { get; set; } = "";
    public string IpAddress { get; set; } = "";
    public bool RdpEnabled { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public class RdpConnection
{
    public string ConnectionId { get; set; } = "";
    public string SessionId { get; set; } = "";
    public int LocalPort { get; set; }
    public RdpConnectionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public System.Net.Sockets.TcpListener? TcpListener { get; set; }
    public System.Net.Sockets.TcpClient? RdpClient { get; set; }
}

public enum RdpConnectionStatus
{
    Connecting,
    Connected,
    Disconnected,
    Error
}

// Message Types
public class AgentRegistration
{
    public SystemInfo SystemInfo { get; set; } = new();
    public string Version { get; set; } = "";
    public List<string> Capabilities { get; set; } = new();
    public int MaxConnections { get; set; }
}

public class AgentRegisteredResponse
{
    public string AgentId { get; set; } = "";
    public string Status { get; set; } = "";
}

public class AgentHeartbeat
{
    public string? AgentId { get; set; }
    public SystemInfo SystemInfo { get; set; } = new();
    public int ActiveConnections { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class SessionStartRequest
{
    public string SessionId { get; set; } = "";
    public string UserId { get; set; } = "";
}

public class SessionStartedResponse
{
    public string SessionId { get; set; } = "";
    public string ConnectionId { get; set; } = "";
    public string Status { get; set; } = "";
}

public class SessionEndRequest
{
    public string SessionId { get; set; } = "";
    public string ConnectionId { get; set; } = "";
}

public class SessionErrorResponse
{
    public string SessionId { get; set; } = "";
    public string Error { get; set; } = "";
}
