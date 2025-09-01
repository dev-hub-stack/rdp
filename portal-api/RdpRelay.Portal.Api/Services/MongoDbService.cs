using MongoDB.Driver;
using Microsoft.Extensions.Options;
using RdpRelay.Portal.Api.Models;

namespace RdpRelay.Portal.Api.Services;

public interface IMongoDbService
{
    IMongoDatabase Database { get; }
    IMongoCollection<T> GetCollection<T>() where T : class;
    IMongoCollection<Tenant> Tenants { get; }
    IMongoCollection<User> Users { get; }
    IMongoCollection<Group> Groups { get; }
    IMongoCollection<Agent> Agents { get; }
    IMongoCollection<Session> Sessions { get; }
    Task EnsureIndexesAsync();
}

public class MongoDbService : IMongoDbService
{
    public IMongoDatabase Database { get; private set; }
    private readonly IMongoClient _client;

    // Collection properties
    public IMongoCollection<Tenant> Tenants { get; private set; }
    public IMongoCollection<User> Users { get; private set; }
    public IMongoCollection<Group> Groups { get; private set; }
    public IMongoCollection<Agent> Agents { get; private set; }
    public IMongoCollection<Session> Sessions { get; private set; }

    public MongoDbService(IOptions<MongoDbOptions> options)
    {
        _client = new MongoClient(options.Value.ConnectionString);
        Database = _client.GetDatabase(options.Value.DatabaseName);
        
        // Initialize collection properties
        Tenants = GetCollection<Tenant>();
        Users = GetCollection<User>();
        Groups = GetCollection<Group>();
        Agents = GetCollection<Agent>();
        Sessions = GetCollection<Session>();
    }

    public IMongoCollection<T> GetCollection<T>() where T : class
    {
        var collectionName = GetCollectionName<T>();
        return Database.GetCollection<T>(collectionName);
    }

    public async Task EnsureIndexesAsync()
    {
        await CreateIndexesAsync();
    }

    private async Task CreateIndexesAsync()
    {
        // Create indexes for better query performance
        
        // User indexes
        var userEmailIndex = Builders<User>.IndexKeys.Ascending(u => u.Email);
        var userTenantIndex = Builders<User>.IndexKeys.Ascending(u => u.TenantId);
        await Users.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<User>(userEmailIndex, new CreateIndexOptions { Unique = true }),
            new CreateIndexModel<User>(userTenantIndex)
        });

        // Tenant indexes  
        var tenantDomainIndex = Builders<Tenant>.IndexKeys.Ascending(t => t.Domain);
        await Tenants.Indexes.CreateOneAsync(new CreateIndexModel<Tenant>(tenantDomainIndex, new CreateIndexOptions { Unique = true }));

        // Group indexes
        var groupTenantIndex = Builders<Group>.IndexKeys.Ascending(g => g.TenantId);
        await Groups.Indexes.CreateOneAsync(new CreateIndexModel<Group>(groupTenantIndex));

        // Agent indexes
        var agentTenantIndex = Builders<Agent>.IndexKeys.Ascending(a => a.TenantId);
        var agentMachineIdIndex = Builders<Agent>.IndexKeys.Ascending(a => a.MachineId);
        var agentHeartbeatIndex = Builders<Agent>.IndexKeys.Descending(a => a.LastHeartbeat);
        await Agents.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<Agent>(agentTenantIndex),
            new CreateIndexModel<Agent>(agentMachineIdIndex),
            new CreateIndexModel<Agent>(agentHeartbeatIndex)
        });

        // Session indexes
        var sessionTenantIndex = Builders<Session>.IndexKeys.Ascending(s => s.TenantId);
        var sessionUserIndex = Builders<Session>.IndexKeys.Ascending(s => s.UserId);
        var sessionCreatedAtIndex = Builders<Session>.IndexKeys.Descending(s => s.CreatedAt);
        await Sessions.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<Session>(sessionTenantIndex),
            new CreateIndexModel<Session>(sessionUserIndex),
            new CreateIndexModel<Session>(sessionCreatedAtIndex)
        });
    }

    private static string GetCollectionName<T>()
    {
        var type = typeof(T);
        var attribute = type.GetCustomAttributes(typeof(BsonCollectionAttribute), true)
            .FirstOrDefault() as BsonCollectionAttribute;
        
        return attribute?.CollectionName ?? type.Name.ToLowerInvariant();
    }
}
