using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RdpRelay.Portal.Api.Services;

namespace RdpRelay.Portal.Api.Controllers;

[ApiController]
[Route("api/sessions")]
[AllowAnonymous]
public class SessionConnectController : ControllerBase
{
    private readonly SessionService _sessionService;
    private readonly ILogger<SessionConnectController> _logger;

    public SessionConnectController(SessionService sessionService, ILogger<SessionConnectController> logger)
    {
        _sessionService = sessionService;
        _logger = logger;
    }

    [HttpGet("connect/{connectCode}")]
    public async Task<ActionResult<object>> GetConnectInfo(string connectCode)
    {
        try
        {
            // For now, return mock connection info since the actual implementation
            // would require Redis for connect code mapping
            // In production, this would validate the connect code and return actual relay endpoint info
            
            if (string.IsNullOrEmpty(connectCode) || connectCode.Length != 8)
            {
                return NotFound();
            }

            // Mock connection info - in production this would come from session/relay service
            var connectInfo = new
            {
                host = "localhost",
                port = 9443
            };

            _logger.LogInformation("Connect info requested for code {ConnectCode}", connectCode);
            return Ok(connectInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving connect info for code {ConnectCode}", connectCode);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
