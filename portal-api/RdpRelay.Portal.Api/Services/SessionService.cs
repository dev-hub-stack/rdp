using MongoDB.Bson;
using MongoDB.Driver;
using RdpRelay.Portal.Api.Models;
using System.Security.Claims;

namespace RdpRelay.Portal.Api.Services;

public class SessionService
{
    private readonly MongoDbService _mongo;
    private readonly IJwtService _jwt;
    private readonly ILogger<SessionService> _logger;

    public SessionService(MongoDbService mongo, IJwtService jwt, ILogger<SessionService> logger)
    {
        _mongo = mongo;
        _jwt = jwt;
        _logger = logger;
    }

    public async Task<CreateSessionResponse> CreateSessionAsync(string tenantId, string userId, CreateSessionRequest request)
    {
        // Validate agent exists and is online
        var agent = await _mongo.Agents.Find(a => 
            a.TenantId == tenantId && 
            a.Id == request.AgentId && 
            a.IsActive)
            .FirstOrDefaultAsync();

        if (agent == null)
            throw new InvalidOperationException("Agent not found or inactive");

        if (agent.Status != AgentStatus.Online)
            throw new InvalidOperationException("Agent is not online");

        // Generate connect code before creating session
        var connectCode = GenerateConnectCode();

        // Create session
        var session = new Session
        {
            Id = ObjectId.GenerateNewId().ToString(),
            TenantId = tenantId,
            UserId = userId,
            AgentId = request.AgentId,
            ClientIp = request.ClientIp,
            UserAgent = request.UserAgent,
            ConnectCode = connectCode,
            Status = SessionStatus.Created,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1) // Default 1 hour expiry
        };

        await _mongo.Sessions.InsertOneAsync(session);

        // Generate session token
        var sessionToken = _jwt.GenerateSessionToken(tenantId, userId, session.Id, agent.Id);

        // Store connect code mapping (in production, use Redis with TTL)
        var codeMapping = new ConnectCodeMapping
        {
            ConnectCode = connectCode,
            SessionToken = sessionToken,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5) // Connect code expires in 5 minutes
        };

        // For now, store in memory or database - in production use Redis
        // This is a simplified implementation
        
        _logger.LogInformation("Created session {SessionId} for user {UserId} to agent {AgentId}", 
            session.Id, userId, request.AgentId);

        return new CreateSessionResponse
        {
            SessionId = session.Id,
            ConnectCode = connectCode,
            ExpiresAt = session.ExpiresAt
        };
    }

    public async Task<Session?> GetSessionAsync(string tenantId, string sessionId)
    {
        return await _mongo.Sessions.Find(s => 
            s.TenantId == tenantId && 
            s.Id == sessionId)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Session>> GetSessionsAsync(string tenantId, SessionFilter? filter = null, int skip = 0, int limit = 50)
    {
        var filterBuilder = Builders<Session>.Filter;
        var filters = new List<FilterDefinition<Session>>
        {
            filterBuilder.Eq(s => s.TenantId, tenantId)
        };

        if (filter != null)
        {
            if (!string.IsNullOrEmpty(filter.UserId))
                filters.Add(filterBuilder.Eq(s => s.UserId, filter.UserId));

            if (!string.IsNullOrEmpty(filter.AgentId))
                filters.Add(filterBuilder.Eq(s => s.AgentId, filter.AgentId));

            if (filter.Status.HasValue)
                filters.Add(filterBuilder.Eq(s => s.Status, filter.Status.Value));

            if (filter.StartDate.HasValue)
                filters.Add(filterBuilder.Gte(s => s.CreatedAt, filter.StartDate.Value));

            if (filter.EndDate.HasValue)
                filters.Add(filterBuilder.Lt(s => s.CreatedAt, filter.EndDate.Value));
        }

        var combinedFilter = filterBuilder.And(filters);
        return await _mongo.Sessions.Find(combinedFilter)
            .SortByDescending(s => s.CreatedAt)
            .Skip(skip)
            .Limit(limit)
            .ToListAsync();
    }

    public async Task<bool> UpdateSessionStatusAsync(string tenantId, string sessionId, SessionStatus status, 
        string? endReason = null)
    {
        var update = Builders<Session>.Update
            .Set(s => s.Status, status)
            .Set(s => s.UpdatedAt, DateTime.UtcNow);

        if (status == SessionStatus.Ended || status == SessionStatus.Failed)
        {
            update = update
                .Set(s => s.EndedAt, DateTime.UtcNow)
                .Set(s => s.EndReason, endReason);
        }
        else if (status == SessionStatus.Connected)
        {
            update = update.Set(s => s.ConnectedAt, DateTime.UtcNow);
        }

        var result = await _mongo.Sessions.UpdateOneAsync(
            s => s.TenantId == tenantId && s.Id == sessionId,
            update);

        if (result.ModifiedCount > 0)
        {
            _logger.LogInformation("Updated session {SessionId} status to {Status}", sessionId, status);
            return true;
        }

        return false;
    }

    public async Task<bool> EndSessionAsync(string tenantId, string sessionId, string reason)
    {
        return await UpdateSessionStatusAsync(tenantId, sessionId, SessionStatus.Ended, reason);
    }

    public async Task<SessionStats> GetSessionStatsAsync(string tenantId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var filterBuilder = Builders<Session>.Filter;
        var filters = new List<FilterDefinition<Session>>
        {
            filterBuilder.Eq(s => s.TenantId, tenantId)
        };

        if (startDate.HasValue)
            filters.Add(filterBuilder.Gte(s => s.CreatedAt, startDate.Value));

        if (endDate.HasValue)
            filters.Add(filterBuilder.Lt(s => s.CreatedAt, endDate.Value));

        var combinedFilter = filterBuilder.And(filters);
        var sessions = await _mongo.Sessions.Find(combinedFilter).ToListAsync();

        return new SessionStats
        {
            Total = sessions.Count,
            Active = sessions.Count(s => s.Status == SessionStatus.Connected),
            Completed = sessions.Count(s => s.Status == SessionStatus.Ended),
            Failed = sessions.Count(s => s.Status == SessionStatus.Failed),
            AverageSessionMinutes = sessions
                .Where(s => s.ConnectedAt.HasValue && s.EndedAt.HasValue)
                .Select(s => (s.EndedAt!.Value - s.ConnectedAt!.Value).TotalMinutes)
                .DefaultIfEmpty(0)
                .Average()
        };
    }

    public async Task<List<Session>> GetActiveSessionsAsync(string tenantId)
    {
        return await _mongo.Sessions.Find(s => 
            s.TenantId == tenantId && 
            s.Status == SessionStatus.Connected)
            .ToListAsync();
    }

    public async Task<bool> CleanupExpiredSessionsAsync()
    {
        var cutoffTime = DateTime.UtcNow;
        var filter = Builders<Session>.Filter.And(
            Builders<Session>.Filter.Lt(s => s.ExpiresAt, cutoffTime),
            Builders<Session>.Filter.In(s => s.Status, new[] { SessionStatus.Created, SessionStatus.Connected })
        );

        var update = Builders<Session>.Update
            .Set(s => s.Status, SessionStatus.Ended)
            .Set(s => s.EndReason, "Session expired")
            .Set(s => s.EndedAt, DateTime.UtcNow)
            .Set(s => s.UpdatedAt, DateTime.UtcNow);

        var result = await _mongo.Sessions.UpdateManyAsync(filter, update);
        
        if (result.ModifiedCount > 0)
        {
            _logger.LogInformation("Cleaned up {Count} expired sessions", result.ModifiedCount);
        }

        return result.ModifiedCount > 0;
    }

    private string GenerateConnectCode()
    {
        // Generate a 8-character alphanumeric code
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 8)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    public string? ValidateConnectCode(string connectCode)
    {
        // In production, this would look up the connect code in Redis
        // For now, this is a simplified implementation
        // Return the session token if valid, null if invalid/expired
        
        // This is a placeholder - implement proper connect code validation
        return null;
    }
}

public class ConnectCodeMapping
{
    public string ConnectCode { get; set; } = string.Empty;
    public string SessionToken { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
