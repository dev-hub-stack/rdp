using MongoDB.Driver;
using RdpRelay.Portal.Api.Models;
using BCrypt.Net;

namespace RdpRelay.Portal.Api.Services;

public interface IUserService
{
    Task<User?> AuthenticateAsync(string email, string password);
    Task<User> CreateUserAsync(CreateUserRequest request, string? createdByUserId = null);
    Task<User?> GetUserByIdAsync(string userId);
    Task<User?> GetUserByEmailAsync(string email);
    Task<IEnumerable<User>> GetUsersByTenantAsync(string tenantId);
    Task UpdateUserAsync(User user);
    Task DeleteUserAsync(string userId);
    Task<User?> GetUserAsync(string tenantId, string userId);
    Task UpdateLastLoginAsync(string userId);
}

public class UserService : IUserService
{
    private readonly IMongoCollection<User> _users;
    private readonly ILogger<UserService> _logger;

    public UserService(MongoDbService mongoDb, ILogger<UserService> logger)
    {
        _users = mongoDb.Users;
        _logger = logger;
    }

    public async Task<User?> AuthenticateAsync(string email, string password)
    {
        var user = await GetUserByEmailAsync(email);
        if (user == null || !user.IsActive)
            return null;

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return null;

        _logger.LogInformation("User {Email} authenticated successfully", email);
        return user;
    }

    public async Task<User> CreateUserAsync(CreateUserRequest request, string? createdByUserId = null)
    {
        // Check if user already exists
        var existingUser = await GetUserByEmailAsync(request.Email);
        if (existingUser != null)
        {
            throw new InvalidOperationException($"User with email {request.Email} already exists");
        }

        var user = new User
        {
            Email = request.Email,
            FirstName = request.FirstName ?? "",
            LastName = request.LastName ?? "",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role,
            TenantId = request.TenantId ?? "",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _users.InsertOneAsync(user);
        
        _logger.LogInformation("Created user {Email} with role {Role} by {CreatedBy}", 
            user.Email, user.Role, createdByUserId ?? "system");

        return user;
    }

    public async Task<User?> GetUserByIdAsync(string userId)
    {
        return await _users.Find(u => u.Id == userId).FirstOrDefaultAsync();
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _users.Find(u => u.Email == email).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<User>> GetUsersByTenantAsync(string tenantId)
    {
        return await _users.Find(u => u.TenantId == tenantId).ToListAsync();
    }

    public async Task UpdateUserAsync(User user)
    {
        await _users.ReplaceOneAsync(u => u.Id == user.Id, user);
        _logger.LogInformation("Updated user {Email}", user.Email);
    }

    public async Task DeleteUserAsync(string userId)
    {
        var result = await _users.DeleteOneAsync(u => u.Id == userId);
        if (result.DeletedCount > 0)
        {
            _logger.LogInformation("Deleted user {UserId}", userId);
        }
    }

    public async Task<User?> GetUserAsync(string tenantId, string userId)
    {
        return await _users.Find(u => u.Id == userId && u.TenantId == tenantId && u.IsActive).FirstOrDefaultAsync();
    }

    public async Task UpdateLastLoginAsync(string userId)
    {
        var update = Builders<User>.Update.Set(u => u.LastLoginAt, DateTime.UtcNow);
        await _users.UpdateOneAsync(u => u.Id == userId, update);
    }
}