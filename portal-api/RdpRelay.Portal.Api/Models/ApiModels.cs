using System.ComponentModel.DataAnnotations;

namespace RdpRelay.Portal.Api.Models;

// Authentication Models
public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    public string Password { get; set; } = "";
}

public class LoginResponse
{
    public string Token { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    public UserInfo User { get; set; } = new();
}

public class RefreshRequest
{
    [Required]
    public string RefreshToken { get; set; } = "";
}

public class RefreshResponse
{
    public string Token { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
}

public class UserInfo
{
    public string Id { get; set; } = "";
    public string Email { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public UserRole Role { get; set; }
    public string TenantId { get; set; } = "";
}

// Pagination Models
public class PagedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int Total { get; set; }
    public int Skip { get; set; }
    public int Limit { get; set; }
}

// Tenant Models
public class CreateTenantRequest
{
    [Required]
    public string Name { get; set; } = "";
    
    [Required]
    public string Domain { get; set; } = "";
    
    public int? MaxAgents { get; set; }
    public int? MaxConcurrentSessions { get; set; }
    public int? SessionTimeoutMinutes { get; set; }
    public List<string>? AllowedIpRanges { get; set; }
}

public class UpdateTenantRequest
{
    public string? Name { get; set; }
    public string? Domain { get; set; }
    public bool? IsActive { get; set; }
    public UpdateTenantSettingsRequest? Settings { get; set; }
}

public class UpdateTenantSettingsRequest
{
    public int? MaxAgents { get; set; }
    public int? MaxConcurrentSessions { get; set; }
    public int? SessionTimeoutMinutes { get; set; }
    public bool? RequireTls { get; set; }
    public List<string>? AllowedIpRanges { get; set; }
}

// Agent Models
public class CreateAgentRequest
{
    [Required]
    public string Name { get; set; } = "";
    
    public string Description { get; set; } = "";
    
    [Required]
    public string MachineId { get; set; } = "";
    
    public string MachineName { get; set; } = "";
    public string IpAddress { get; set; } = "";
    public int? RdpPort { get; set; }
    public List<string>? GroupIds { get; set; }
    public Dictionary<string, string>? Tags { get; set; }
}

public class UpdateAgentRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? IpAddress { get; set; }
    public int? RdpPort { get; set; }
    public List<string>? GroupIds { get; set; }
    public Dictionary<string, string>? Tags { get; set; }
    public bool? IsActive { get; set; }
}

public class AgentFilter
{
    public string? GroupId { get; set; }
    public AgentStatus? Status { get; set; }
    public string? Search { get; set; }
}

public class AgentStats
{
    public int Total { get; set; }
    public int Online { get; set; }
    public int Offline { get; set; }
    public int InSession { get; set; }
}

// Session Models
public class CreateSessionRequest
{
    [Required]
    public string AgentId { get; set; } = "";
    
    public string? ClientIp { get; set; }
    public string? UserAgent { get; set; }
}

public class CreateSessionResponse
{
    public string SessionId { get; set; } = "";
    public string ConnectCode { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
}

public class UpdateSessionStatusRequest
{
    [Required]
    public SessionStatus Status { get; set; }
    
    public string? EndReason { get; set; }
}

public class EndSessionRequest
{
    public string? Reason { get; set; }
}

public class SessionFilter
{
    public string? UserId { get; set; }
    public string? AgentId { get; set; }
    public SessionStatus? Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class SessionStats
{
    public int Total { get; set; }
    public int Active { get; set; }
    public int Completed { get; set; }
    public int Failed { get; set; }
    public double AverageSessionMinutes { get; set; }
}

// User Management Models
public class CreateUserRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = "";

    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = "";

    [Required]
    public UserRole Role { get; set; } = UserRole.Operator;

    public string? TenantId { get; set; }
}

public class UpdateUserRequest
{
    [EmailAddress]
    public string? Email { get; set; }

    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    [MinLength(8)]
    public string? Password { get; set; }

    public UserRole? Role { get; set; }

    public bool? IsActive { get; set; }
}

public class CreateGroupRequest
{
    [Required]
    public string Name { get; set; } = "";
    
    public string Description { get; set; } = "";
}

public class CreateAgentResponse
{
    public string AgentId { get; set; } = "";
    public string AgentKey { get; set; } = "";
    public string ProvisioningJwt { get; set; } = "";
}

public class SessionRequestRequest
{
    [Required]
    public string AgentId { get; set; } = "";
}

public class SessionRequestResponse
{
    public string SessionId { get; set; } = "";
    public string SessionJwt { get; set; } = "";
    public string? ConnectCode { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string RelayEndpoint { get; set; } = "";
}

public class AgentDto
{
    public string Id { get; set; } = "";
    public string Hostname { get; set; } = "";
    public string? GroupName { get; set; }
    public AgentStatus Status { get; set; }
    public DateTime? LastHeartbeatAt { get; set; }
    public DateTime? ConnectedAt { get; set; }
    public string Notes { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class SessionDto
{
    public string Id { get; set; } = "";
    public string SessionId { get; set; } = "";
    public string AgentHostname { get; set; } = "";
    public string OperatorEmail { get; set; } = "";
    public SessionStatus Status { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public long BytesUp { get; set; }
    public long BytesDown { get; set; }
    public string? Reason { get; set; }
}

public class GenerateProvisioningTokenRequest
{
    public string? GroupId { get; set; }
}

public class StatsOverviewResponse
{
    public int TotalTenants { get; set; }
    public int TotalUsers { get; set; }
    public int TotalAgents { get; set; }
    public int OnlineAgents { get; set; }
    public int ActiveSessions { get; set; }
    public long TotalSessionsToday { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
