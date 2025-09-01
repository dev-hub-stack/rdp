using RdpRelay.Relay.Models;
using System.Collections.Concurrent;

namespace RdpRelay.Relay.Services;

public interface ISessionBroker
{
    Task<SessionRequest> CreateSessionRequestAsync(string agentId, string tenantId, string operatorUserId);
    Task<SessionRequest?> GetSessionRequestAsync(string sessionId);
    Task<SessionRequest?> PairSessionAsync(string sessionId, string? connectCode = null);
    Task CompleteSessionAsync(string sessionId, long bytesUp = 0, long bytesDown = 0, string? reason = null);
    Task<int> GetActiveSessionCountAsync();
    Task CleanupExpiredSessionsAsync();
}

public class SessionBroker : ISessionBroker
{
    private readonly ConcurrentDictionary<string, SessionRequest> _sessions = new();
    private readonly ILogger<SessionBroker> _logger;
    private static readonly Random _random = new();

    public SessionBroker(ILogger<SessionBroker> logger)
    {
        _logger = logger;
    }

    public Task<SessionRequest> CreateSessionRequestAsync(string agentId, string tenantId, string operatorUserId)
    {
        var sessionId = GenerateSessionId();
        var connectCode = GenerateConnectCode();
        
        var session = new SessionRequest
        {
            SessionId = sessionId,
            AgentId = agentId,
            TenantId = tenantId,
            OperatorUserId = operatorUserId,
            ConnectCode = connectCode,
            RequestedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5), // Short TTL for security
            Status = SessionStatus.Requested
        };

        _sessions.TryAdd(sessionId, session);
        
        _logger.LogInformation("Created session request {SessionId} for agent {AgentId} by operator {OperatorUserId}", 
            sessionId, agentId, operatorUserId);

        return Task.FromResult(session);
    }

    public Task<SessionRequest?> GetSessionRequestAsync(string sessionId)
    {
        _sessions.TryGetValue(sessionId, out var session);
        return Task.FromResult(session);
    }

    public Task<SessionRequest?> PairSessionAsync(string sessionId, string? connectCode = null)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
            return Task.FromResult<SessionRequest?>(null);

        if (session.Status != SessionStatus.Requested)
            return Task.FromResult<SessionRequest?>(null);

        if (DateTime.UtcNow > session.ExpiresAt)
        {
            session.Status = SessionStatus.Expired;
            return Task.FromResult<SessionRequest?>(null);
        }

        // Validate connect code if required
        if (!string.IsNullOrEmpty(session.ConnectCode) && session.ConnectCode != connectCode)
        {
            _logger.LogWarning("Invalid connect code for session {SessionId}", sessionId);
            return Task.FromResult<SessionRequest?>(null);
        }

        session.Status = SessionStatus.Paired;
        
        _logger.LogInformation("Paired session {SessionId} for agent {AgentId}", sessionId, session.AgentId);
        
        return Task.FromResult<SessionRequest?>(session);
    }

    public Task CompleteSessionAsync(string sessionId, long bytesUp = 0, long bytesDown = 0, string? reason = null)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            session.Status = SessionStatus.Ended;
            
            _logger.LogInformation("Session {SessionId} completed. Bytes: Up={BytesUp}, Down={BytesDown}, Reason={Reason}", 
                sessionId, bytesUp, bytesDown, reason ?? "Normal");
            
            // Remove completed session after a delay to allow for reporting
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromMinutes(5));
                _sessions.TryRemove(sessionId, out _);
            });
        }

        return Task.CompletedTask;
    }

    public Task<int> GetActiveSessionCountAsync()
    {
        return Task.FromResult(_sessions.Count(s => s.Value.Status == SessionStatus.Active || s.Value.Status == SessionStatus.Paired));
    }

    public Task CleanupExpiredSessionsAsync()
    {
        var now = DateTime.UtcNow;
        var expiredSessions = _sessions.Where(s => now > s.Value.ExpiresAt && s.Value.Status == SessionStatus.Requested).ToList();
        
        foreach (var (sessionId, session) in expiredSessions)
        {
            session.Status = SessionStatus.Expired;
            _sessions.TryRemove(sessionId, out _);
            _logger.LogInformation("Expired session {SessionId} for agent {AgentId}", sessionId, session.AgentId);
        }

        return Task.CompletedTask;
    }

    private static string GenerateSessionId()
    {
        return $"S-{Guid.NewGuid():N}";
    }

    private static string GenerateConnectCode()
    {
        return _random.Next(100000, 999999).ToString();
    }
}
