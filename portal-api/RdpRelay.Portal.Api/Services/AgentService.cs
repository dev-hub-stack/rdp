using MongoDB.Bson;
using MongoDB.Driver;
using RdpRelay.Portal.Api.Models;
using System.Security.Claims;

namespace RdpRelay.Portal.Api.Services;

public class AgentService
{
    private readonly MongoDbService _mongo;
    private readonly ILogger<AgentService> _logger;

    public AgentService(MongoDbService mongo, ILogger<AgentService> logger)
    {
        _mongo = mongo;
        _logger = logger;
    }

    public async Task<Agent> CreateAgentAsync(string tenantId, CreateAgentRequest request)
    {
        var agent = new Agent
        {
            Id = ObjectId.GenerateNewId().ToString(),
            TenantId = tenantId,
            Name = request.Name,
            Description = request.Description,
            MachineId = request.MachineId,
            MachineName = request.MachineName,
            IpAddress = request.IpAddress,
            RdpPort = request.RdpPort ?? 3389,
            GroupIds = request.GroupIds ?? new List<string>(),
            Tags = request.Tags ?? new Dictionary<string, string>(),
            IsActive = true,
            Status = AgentStatus.Offline,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _mongo.Agents.InsertOneAsync(agent);
        _logger.LogInformation("Created agent {AgentId} ({AgentName}) for tenant {TenantId}", 
            agent.Id, agent.Name, tenantId);
        return agent;
    }

    public async Task<Agent?> GetAgentAsync(string tenantId, string agentId)
    {
        return await _mongo.Agents.Find(a => a.TenantId == tenantId && a.Id == agentId && a.IsActive)
            .FirstOrDefaultAsync();
    }

    public async Task<Agent?> GetAgentByMachineIdAsync(string tenantId, string machineId)
    {
        return await _mongo.Agents.Find(a => a.TenantId == tenantId && a.MachineId == machineId && a.IsActive)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Agent>> GetAgentsAsync(string tenantId, AgentFilter? filter = null, int skip = 0, int limit = 50)
    {
        var filterBuilder = Builders<Agent>.Filter;
        var filters = new List<FilterDefinition<Agent>>
        {
            filterBuilder.Eq(a => a.TenantId, tenantId),
            filterBuilder.Eq(a => a.IsActive, true)
        };

        if (filter != null)
        {
            if (!string.IsNullOrEmpty(filter.GroupId))
                filters.Add(filterBuilder.AnyEq(a => a.GroupIds, filter.GroupId));

            if (filter.Status.HasValue)
                filters.Add(filterBuilder.Eq(a => a.Status, filter.Status.Value));

            if (!string.IsNullOrEmpty(filter.Search))
            {
                var searchFilter = filterBuilder.Or(
                    filterBuilder.Regex(a => a.Name, new MongoDB.Bson.BsonRegularExpression(filter.Search, "i")),
                    filterBuilder.Regex(a => a.MachineName, new MongoDB.Bson.BsonRegularExpression(filter.Search, "i")),
                    filterBuilder.Regex(a => a.IpAddress, new MongoDB.Bson.BsonRegularExpression(filter.Search, "i"))
                );
                filters.Add(searchFilter);
            }
        }

        var combinedFilter = filterBuilder.And(filters);
        return await _mongo.Agents.Find(combinedFilter)
            .SortBy(a => a.Name)
            .Skip(skip)
            .Limit(limit)
            .ToListAsync();
    }

    public async Task<Agent?> UpdateAgentAsync(string tenantId, string agentId, UpdateAgentRequest request)
    {
        var update = Builders<Agent>.Update
            .Set(a => a.UpdatedAt, DateTime.UtcNow);

        if (!string.IsNullOrEmpty(request.Name))
            update = update.Set(a => a.Name, request.Name);

        if (!string.IsNullOrEmpty(request.Description))
            update = update.Set(a => a.Description, request.Description);

        if (!string.IsNullOrEmpty(request.IpAddress))
            update = update.Set(a => a.IpAddress, request.IpAddress);

        if (request.RdpPort.HasValue)
            update = update.Set(a => a.RdpPort, request.RdpPort.Value);

        if (request.GroupIds != null)
            update = update.Set(a => a.GroupIds, request.GroupIds);

        if (request.Tags != null)
            update = update.Set(a => a.Tags, request.Tags);

        if (request.IsActive.HasValue)
            update = update.Set(a => a.IsActive, request.IsActive.Value);

        var result = await _mongo.Agents.FindOneAndUpdateAsync(
            a => a.TenantId == tenantId && a.Id == agentId,
            update,
            new FindOneAndUpdateOptions<Agent> { ReturnDocument = ReturnDocument.After });

        if (result != null)
            _logger.LogInformation("Updated agent {AgentId} for tenant {TenantId}", agentId, tenantId);

        return result;
    }

    public async Task<bool> DeleteAgentAsync(string tenantId, string agentId)
    {
        var update = Builders<Agent>.Update
            .Set(a => a.IsActive, false)
            .Set(a => a.UpdatedAt, DateTime.UtcNow);

        var result = await _mongo.Agents.UpdateOneAsync(
            a => a.TenantId == tenantId && a.Id == agentId, 
            update);

        if (result.ModifiedCount > 0)
        {
            _logger.LogInformation("Deactivated agent {AgentId} for tenant {TenantId}", agentId, tenantId);
            return true;
        }

        return false;
    }

    public async Task<bool> UpdateAgentStatusAsync(string tenantId, string agentId, AgentStatus status, 
        string? version = null, DateTime? lastHeartbeat = null)
    {
        var update = Builders<Agent>.Update
            .Set(a => a.Status, status)
            .Set(a => a.UpdatedAt, DateTime.UtcNow);

        if (!string.IsNullOrEmpty(version))
            update = update.Set(a => a.Version, version);

        if (lastHeartbeat.HasValue)
            update = update.Set(a => a.LastHeartbeat, lastHeartbeat.Value);

        var result = await _mongo.Agents.UpdateOneAsync(
            a => a.TenantId == tenantId && a.Id == agentId,
            update);

        return result.ModifiedCount > 0;
    }

    public async Task<List<Agent>> GetOnlineAgentsAsync(string tenantId)
    {
        var cutoffTime = DateTime.UtcNow.AddMinutes(-5); // Consider offline after 5 minutes
        
        return await _mongo.Agents.Find(a => 
            a.TenantId == tenantId && 
            a.IsActive && 
            a.Status == AgentStatus.Online &&
            a.LastHeartbeat > cutoffTime)
            .ToListAsync();
    }

    public async Task<AgentStats> GetAgentStatsAsync(string tenantId)
    {
        var agents = await _mongo.Agents.Find(a => a.TenantId == tenantId && a.IsActive).ToListAsync();
        var cutoffTime = DateTime.UtcNow.AddMinutes(-5);

        return new AgentStats
        {
            Total = agents.Count,
            Online = agents.Count(a => a.Status == AgentStatus.Online && a.LastHeartbeat > cutoffTime),
            Offline = agents.Count(a => a.Status != AgentStatus.Online || a.LastHeartbeat <= cutoffTime),
            InSession = agents.Count(a => a.Status == AgentStatus.InSession)
        };
    }
}
