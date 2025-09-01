using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RdpRelay.Portal.Api.Models;
using RdpRelay.Portal.Api.Services;
using System.Security.Claims;

namespace RdpRelay.Portal.Api.Controllers;

[ApiController]
[Route("api/tenants/{tenantId}/[controller]")]
[Authorize]
public class SessionsController : ControllerBase
{
    private readonly SessionService _sessionService;
    private readonly TenantService _tenantService;
    private readonly ILogger<SessionsController> _logger;

    public SessionsController(SessionService sessionService, TenantService tenantService, ILogger<SessionsController> logger)
    {
        _sessionService = sessionService;
        _tenantService = tenantService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<Session>>> GetSessions(
        string tenantId,
        [FromQuery] string? userId = null,
        [FromQuery] string? agentId = null,
        [FromQuery] SessionStatus? status = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
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

            var filter = new SessionFilter
            {
                UserId = userId,
                AgentId = agentId,
                Status = status,
                StartDate = startDate,
                EndDate = endDate
            };

            var sessions = await _sessionService.GetSessionsAsync(tenantId, filter, skip, limit);
            var total = sessions.Count; // In production, get actual count

            return Ok(new PagedResponse<Session>
            {
                Items = sessions,
                Total = total,
                Skip = skip,
                Limit = limit
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sessions for tenant {TenantId}", tenantId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{sessionId}")]
    public async Task<ActionResult<Session>> GetSession(string tenantId, string sessionId)
    {
        try
        {
            // Validate tenant access
            if (!await _tenantService.ValidateTenantAccessAsync(User, tenantId))
            {
                return Forbid();
            }

            var session = await _sessionService.GetSessionAsync(tenantId, sessionId);
            if (session == null)
            {
                return NotFound();
            }

            // Users can only see their own sessions unless they're admin/operator
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdminOrOperator = User.IsInRole("TenantAdmin") || User.IsInRole("Operator");
            
            if (!isAdminOrOperator && session.UserId != currentUserId)
            {
                return Forbid();
            }

            return Ok(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving session {SessionId} for tenant {TenantId}", sessionId, tenantId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost]
    public async Task<ActionResult<CreateSessionResponse>> CreateSession(string tenantId, [FromBody] CreateSessionRequest request)
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

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Set client IP if not provided
            if (string.IsNullOrEmpty(request.ClientIp))
            {
                request.ClientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            }

            // Set user agent if not provided
            if (string.IsNullOrEmpty(request.UserAgent))
            {
                request.UserAgent = HttpContext.Request.Headers.UserAgent.ToString();
            }

            var response = await _sessionService.CreateSessionAsync(tenantId, userId, request);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating session for tenant {TenantId}", tenantId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("{sessionId}/status")]
    [Authorize(Roles = "TenantAdmin,Operator")]
    public async Task<ActionResult> UpdateSessionStatus(string tenantId, string sessionId, [FromBody] UpdateSessionStatusRequest request)
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

            var success = await _sessionService.UpdateSessionStatusAsync(tenantId, sessionId, request.Status, request.EndReason);
            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating session {SessionId} status for tenant {TenantId}", sessionId, tenantId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("{sessionId}/end")]
    public async Task<ActionResult> EndSession(string tenantId, string sessionId, [FromBody] EndSessionRequest request)
    {
        try
        {
            // Validate tenant access
            if (!await _tenantService.ValidateTenantAccessAsync(User, tenantId))
            {
                return Forbid();
            }

            // Get the session to check ownership
            var session = await _sessionService.GetSessionAsync(tenantId, sessionId);
            if (session == null)
            {
                return NotFound();
            }

            // Users can only end their own sessions unless they're admin/operator
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdminOrOperator = User.IsInRole("TenantAdmin") || User.IsInRole("Operator");
            
            if (!isAdminOrOperator && session.UserId != currentUserId)
            {
                return Forbid();
            }

            var reason = request.Reason ?? "User requested";
            var success = await _sessionService.EndSessionAsync(tenantId, sessionId, reason);
            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending session {SessionId} for tenant {TenantId}", sessionId, tenantId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("stats")]
    public async Task<ActionResult<SessionStats>> GetSessionStats(
        string tenantId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            // Validate tenant access
            if (!await _tenantService.ValidateTenantAccessAsync(User, tenantId))
            {
                return Forbid();
            }

            var stats = await _sessionService.GetSessionStatsAsync(tenantId, startDate, endDate);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving session stats for tenant {TenantId}", tenantId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("active")]
    public async Task<ActionResult<List<Session>>> GetActiveSessions(string tenantId)
    {
        try
        {
            // Validate tenant access
            if (!await _tenantService.ValidateTenantAccessAsync(User, tenantId))
            {
                return Forbid();
            }

            var sessions = await _sessionService.GetActiveSessionsAsync(tenantId);
            return Ok(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active sessions for tenant {TenantId}", tenantId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}


