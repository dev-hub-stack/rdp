using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RdpRelay.Portal.Api.Models;
using RdpRelay.Portal.Api.Services;
using System.Security.Claims;

namespace RdpRelay.Portal.Api.Controllers;

[ApiController]
[Route("api/tenants/{tenantId}/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly TenantService _tenantService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, TenantService tenantService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _tenantService = tenantService;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "SystemAdmin,TenantAdmin")]
    public async Task<ActionResult<PagedResponse<User>>> GetUsers(
        string tenantId,
        [FromQuery] string? search = null,
        [FromQuery] UserRole? role = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 50)
    {
        try
        {
            // Validate tenant access
            if (!await _tenantService.ValidateTenantAccessAsync(User, tenantId))
            {
                return Forbid();
            }

            if (limit > 100) limit = 100; // Cap the limit

            var users = await _userService.GetUsersByTenantAsync(tenantId);
            
            // Apply filters
            var filteredUsers = users.Where(u => u.IsActive || isActive != true);
            
            if (!string.IsNullOrEmpty(search))
            {
                var searchLower = search.ToLower();
                filteredUsers = filteredUsers.Where(u => 
                    u.Email.ToLower().Contains(searchLower) ||
                    u.FirstName.ToLower().Contains(searchLower) ||
                    u.LastName.ToLower().Contains(searchLower));
            }
            
            if (role.HasValue)
            {
                filteredUsers = filteredUsers.Where(u => u.Role == role.Value);
            }
            
            if (isActive.HasValue)
            {
                filteredUsers = filteredUsers.Where(u => u.IsActive == isActive.Value);
            }

            var usersList = filteredUsers.Skip(skip).Take(limit).ToList();
            var total = filteredUsers.Count();

            return Ok(new PagedResponse<User>
            {
                Items = usersList,
                Total = total,
                Skip = skip,
                Limit = limit
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users for tenant {TenantId}", tenantId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{userId}")]
    public async Task<ActionResult<User>> GetUser(string tenantId, string userId)
    {
        try
        {
            // Validate tenant access
            if (!await _tenantService.ValidateTenantAccessAsync(User, tenantId))
            {
                return Forbid();
            }

            var user = await _userService.GetUserAsync(tenantId, userId);
            if (user == null)
            {
                return NotFound();
            }

            // Users can only see their own profile unless they're admin
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = User.IsInRole("SystemAdmin") || User.IsInRole("TenantAdmin");
            
            if (!isAdmin && currentUserId != userId)
            {
                return Forbid();
            }

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserId} for tenant {TenantId}", userId, tenantId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost]
    [Authorize(Roles = "SystemAdmin,TenantAdmin")]
    public async Task<ActionResult<User>> CreateUser(string tenantId, [FromBody] CreateUserRequest request)
    {
        try
        {
            // Validate tenant access
            if (!await _tenantService.ValidateTenantAccessAsync(User, tenantId))
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            // Set tenant ID to the current tenant
            request.TenantId = tenantId;
            
            var user = await _userService.CreateUserAsync(request, currentUserId);
            return CreatedAtAction(nameof(GetUser), new { tenantId, userId = user.Id }, user);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user for tenant {TenantId}", tenantId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("{userId}")]
    [Authorize(Roles = "SystemAdmin,TenantAdmin")]
    public async Task<ActionResult<User>> UpdateUser(string tenantId, string userId, [FromBody] UpdateUserRequest request)
    {
        try
        {
            // Validate tenant access
            if (!await _tenantService.ValidateTenantAccessAsync(User, tenantId))
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingUser = await _userService.GetUserAsync(tenantId, userId);
            if (existingUser == null)
            {
                return NotFound();
            }

            // Update user properties
            if (!string.IsNullOrEmpty(request.Email))
                existingUser.Email = request.Email;
            if (!string.IsNullOrEmpty(request.FirstName))
                existingUser.FirstName = request.FirstName;
            if (!string.IsNullOrEmpty(request.LastName))
                existingUser.LastName = request.LastName;
            if (request.Role.HasValue)
                existingUser.Role = request.Role.Value;
            if (request.IsActive.HasValue)
                existingUser.IsActive = request.IsActive.Value;
            
            existingUser.UpdatedAt = DateTime.UtcNow;

            // Update password if provided
            if (!string.IsNullOrEmpty(request.Password))
            {
                existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            }

            await _userService.UpdateUserAsync(existingUser);
            return Ok(existingUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId} for tenant {TenantId}", userId, tenantId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpDelete("{userId}")]
    [Authorize(Roles = "SystemAdmin,TenantAdmin")]
    public async Task<ActionResult> DeleteUser(string tenantId, string userId)
    {
        try
        {
            // Validate tenant access
            if (!await _tenantService.ValidateTenantAccessAsync(User, tenantId))
            {
                return Forbid();
            }

            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == userId)
            {
                return BadRequest(new { message = "You cannot delete your own account" });
            }

            var existingUser = await _userService.GetUserAsync(tenantId, userId);
            if (existingUser == null)
            {
                return NotFound();
            }

            await _userService.DeleteUserAsync(userId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId} for tenant {TenantId}", userId, tenantId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
