using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RdpRelay.Portal.Api.Models;
using RdpRelay.Portal.Api.Services;
using System.Security.Claims;

namespace RdpRelay.Portal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly Services.IUserService _userService;
    private readonly Services.IJwtService _jwtService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(Services.IUserService userService, Services.IJwtService jwtService, ILogger<AuthController> logger)
    {
        _userService = userService;
        _jwtService = jwtService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var user = await _userService.AuthenticateAsync(request.Email, request.Password);
            if (user == null)
            {
                _logger.LogWarning("Failed login attempt for {Email} from {IP}", 
                    request.Email, HttpContext.Connection.RemoteIpAddress);
                return Unauthorized(new { message = "Invalid email or password" });
            }

            var token = _jwtService.GenerateAccessToken(user.TenantId, user.Id, user.Role.ToString());
            var refreshToken = _jwtService.GenerateRefreshToken();

            // Store refresh token (in production, use Redis)
            await _userService.UpdateLastLoginAsync(user.Id);

            _logger.LogInformation("User {UserId} logged in successfully", user.Id);

            return Ok(new LoginResponse
            {
                Token = token,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                User = new UserInfo
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role,
                    TenantId = user.TenantId
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for {Email}", request.Email);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<RefreshResponse>> RefreshToken([FromBody] RefreshRequest request)
    {
        try
        {
            // Validate refresh token (simplified - in production, validate against stored tokens)
            var principal = _jwtService.ValidateRefreshToken(request.RefreshToken);
            if (principal == null)
            {
                return Unauthorized(new { message = "Invalid refresh token" });
            }

            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var tenantId = principal.FindFirst("tenant_id")?.Value;
            var role = principal.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(role))
            {
                return Unauthorized(new { message = "Invalid token claims" });
            }

            var user = await _userService.GetUserAsync(tenantId, userId);
            if (user == null || !user.IsActive)
            {
                return Unauthorized(new { message = "User not found or inactive" });
            }

            var newToken = _jwtService.GenerateAccessToken(tenantId, userId, role);
            var newRefreshToken = _jwtService.GenerateRefreshToken();

            return Ok(new RefreshResponse
            {
                Token = newToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult> Logout()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            // In production, invalidate the refresh token in Redis
            _logger.LogInformation("User {UserId} logged out", userId);
        }

        return Ok(new { message = "Logged out successfully" });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserInfo>> GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var tenantId = User.FindFirst("tenant_id")?.Value;

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tenantId))
        {
            return Unauthorized();
        }

        var user = await _userService.GetUserAsync(tenantId, userId);
        if (user == null)
        {
            return NotFound();
        }

        return Ok(new UserInfo
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role,
            TenantId = user.TenantId
        });
    }
}
