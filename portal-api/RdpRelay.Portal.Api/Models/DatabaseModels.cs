using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace RdpRelay.Portal.Api.Models;

[BsonCollection("tenants")]
public class Tenant
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = "";

    [BsonElement("name")]
    public string Name { get; set; } = "";

    [BsonElement("domain")]
    public string Domain { get; set; } = "";

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("settings")]
    public TenantSettings Settings { get; set; } = new();
}

public class TenantSettings
{
    [BsonElement("maxAgents")]
    public int MaxAgents { get; set; } = 100;

    [BsonElement("maxConcurrentSessions")]
    public int MaxConcurrentSessions { get; set; } = 50;

    [BsonElement("sessionTimeoutMinutes")]
    public int SessionTimeoutMinutes { get; set; } = 60;

    [BsonElement("requireTls")]
    public bool RequireTls { get; set; } = true;

    [BsonElement("allowedIpRanges")]
    public List<string> AllowedIpRanges { get; set; } = new();
}

[BsonCollection("users")]
public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = "";

    [BsonElement("tenantId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string TenantId { get; set; } = "";

    [BsonElement("email")]
    public string Email { get; set; } = "";

    [BsonElement("firstName")]
    public string FirstName { get; set; } = "";

    [BsonElement("lastName")]
    public string LastName { get; set; } = "";

    [BsonElement("passwordHash")]
    public string PasswordHash { get; set; } = "";

    [BsonElement("role")]
    public UserRole Role { get; set; } = UserRole.Operator;

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("lastLoginAt")]
    public DateTime? LastLoginAt { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserRole
{
    SystemAdmin,
    TenantAdmin,
    Operator
}

[BsonCollection("groups")]
public class Group
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = "";

    [BsonElement("tenantId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string TenantId { get; set; } = "";

    [BsonElement("name")]
    public string Name { get; set; } = "";

    [BsonElement("description")]
    public string Description { get; set; } = "";

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

[BsonCollection("agents")]
public class Agent
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = "";

    [BsonElement("tenantId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string TenantId { get; set; } = "";

    [BsonElement("name")]
    public string Name { get; set; } = "";

    [BsonElement("description")]
    public string Description { get; set; } = "";

    [BsonElement("machineId")]
    public string MachineId { get; set; } = "";

    [BsonElement("machineName")]
    public string MachineName { get; set; } = "";

    [BsonElement("ipAddress")]
    public string IpAddress { get; set; } = "";

    [BsonElement("rdpPort")]
    public int RdpPort { get; set; } = 3389;

    [BsonElement("groupIds")]
    public List<string> GroupIds { get; set; } = new();

    [BsonElement("tags")]
    public Dictionary<string, string> Tags { get; set; } = new();

    [BsonElement("agentKey")]
    public string AgentKey { get; set; } = "";

    [BsonElement("version")]
    public string? Version { get; set; }

    [BsonElement("status")]
    public AgentStatus Status { get; set; } = AgentStatus.Offline;

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("lastHeartbeat")]
    public DateTime? LastHeartbeat { get; set; }

    [BsonElement("connectedAt")]
    public DateTime? ConnectedAt { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentStatus
{
    Offline,
    Online,
    InSession,
    Error
}

[BsonCollection("sessions")]
public class Session
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = "";

    [BsonElement("tenantId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string TenantId { get; set; } = "";

    [BsonElement("userId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = "";

    [BsonElement("agentId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string AgentId { get; set; } = "";

    [BsonElement("clientIp")]
    public string? ClientIp { get; set; }

    [BsonElement("userAgent")]
    public string? UserAgent { get; set; }

    [BsonElement("status")]
    public SessionStatus Status { get; set; } = SessionStatus.Requested;

    [BsonElement("connectCode")]
    public string? ConnectCode { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("connectedAt")]
    public DateTime? ConnectedAt { get; set; }

    [BsonElement("endedAt")]
    public DateTime? EndedAt { get; set; }

    [BsonElement("expiresAt")]
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(1);

    [BsonElement("endReason")]
    public string? EndReason { get; set; }

    [BsonElement("bytesUp")]
    public long BytesUp { get; set; }

    [BsonElement("bytesDown")]
    public long BytesDown { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SessionStatus
{
    Requested,
    Created,
    Connected,
    Ended,
    Failed,
    Expired
}

// Attribute for MongoDB collection mapping
public class BsonCollectionAttribute : Attribute
{
    public string CollectionName { get; }

    public BsonCollectionAttribute(string collectionName)
    {
        CollectionName = collectionName;
    }
}
