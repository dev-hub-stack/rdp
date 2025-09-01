using MongoDB.Bson;
using MongoDB.Driver;
using RdpRelay.Portal.Api.Models;
using System.Security.Claims;

namespace RdpRelay.Portal.Api.Services;

public class TenantService
{
    private readonly MongoDbService _mongo;
    private readonly ILogger<TenantService> _logger;

    public TenantService(MongoDbService mongo, ILogger<TenantService> logger)
    {
        _mongo = mongo;
        _logger = logger;
    }

    public async Task<Tenant> CreateTenantAsync(CreateTenantRequest request)
    {
        var tenant = new Tenant
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Name = request.Name,
            Domain = request.Domain,
            IsActive = true,
            Settings = new TenantSettings
            {
                MaxAgents = request.MaxAgents ?? 100,
                MaxConcurrentSessions = request.MaxConcurrentSessions ?? 50,
                SessionTimeoutMinutes = request.SessionTimeoutMinutes ?? 60,
                RequireTls = true,
                AllowedIpRanges = request.AllowedIpRanges ?? new List<string>()
            },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _mongo.Tenants.InsertOneAsync(tenant);
        _logger.LogInformation("Created tenant {TenantId} ({TenantName})", tenant.Id, tenant.Name);
        return tenant;
    }

    public async Task<Tenant?> GetTenantAsync(string tenantId)
    {
        return await _mongo.Tenants.Find(t => t.Id == tenantId && t.IsActive).FirstOrDefaultAsync();
    }

    public async Task<Tenant?> GetTenantByDomainAsync(string domain)
    {
        return await _mongo.Tenants.Find(t => t.Domain == domain && t.IsActive).FirstOrDefaultAsync();
    }

    public async Task<List<Tenant>> GetTenantsAsync(int skip = 0, int limit = 50)
    {
        return await _mongo.Tenants
            .Find(t => t.IsActive)
            .SortBy(t => t.Name)
            .Skip(skip)
            .Limit(limit)
            .ToListAsync();
    }

    public async Task<Tenant?> UpdateTenantAsync(string tenantId, UpdateTenantRequest request)
    {
        var update = Builders<Tenant>.Update
            .Set(t => t.UpdatedAt, DateTime.UtcNow);

        if (!string.IsNullOrEmpty(request.Name))
            update = update.Set(t => t.Name, request.Name);

        if (!string.IsNullOrEmpty(request.Domain))
            update = update.Set(t => t.Domain, request.Domain);

        if (request.IsActive.HasValue)
            update = update.Set(t => t.IsActive, request.IsActive.Value);

        if (request.Settings != null)
        {
            if (request.Settings.MaxAgents.HasValue)
                update = update.Set(t => t.Settings.MaxAgents, request.Settings.MaxAgents.Value);
            if (request.Settings.MaxConcurrentSessions.HasValue)
                update = update.Set(t => t.Settings.MaxConcurrentSessions, request.Settings.MaxConcurrentSessions.Value);
            if (request.Settings.SessionTimeoutMinutes.HasValue)
                update = update.Set(t => t.Settings.SessionTimeoutMinutes, request.Settings.SessionTimeoutMinutes.Value);
            if (request.Settings.RequireTls.HasValue)
                update = update.Set(t => t.Settings.RequireTls, request.Settings.RequireTls.Value);
            if (request.Settings.AllowedIpRanges != null)
                update = update.Set(t => t.Settings.AllowedIpRanges, request.Settings.AllowedIpRanges);
        }

        var result = await _mongo.Tenants.FindOneAndUpdateAsync(
            t => t.Id == tenantId,
            update,
            new FindOneAndUpdateOptions<Tenant> { ReturnDocument = ReturnDocument.After });

        if (result != null)
            _logger.LogInformation("Updated tenant {TenantId}", tenantId);

        return result;
    }

    public async Task<bool> DeleteTenantAsync(string tenantId)
    {
        var update = Builders<Tenant>.Update
            .Set(t => t.IsActive, false)
            .Set(t => t.UpdatedAt, DateTime.UtcNow);

        var result = await _mongo.Tenants.UpdateOneAsync(t => t.Id == tenantId, update);
        
        if (result.ModifiedCount > 0)
        {
            _logger.LogInformation("Deactivated tenant {TenantId}", tenantId);
            return true;
        }

        return false;
    }

    public string? GetTenantIdFromClaims(ClaimsPrincipal user)
    {
        return user.FindFirst("tenant_id")?.Value;
    }

    public async Task<bool> ValidateTenantAccessAsync(ClaimsPrincipal user, string tenantId)
    {
        var userTenantId = GetTenantIdFromClaims(user);
        if (userTenantId != tenantId)
            return false;

        var tenant = await GetTenantAsync(tenantId);
        return tenant?.IsActive == true;
    }
}
