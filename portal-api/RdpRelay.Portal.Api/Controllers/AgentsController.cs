using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using RdpRelay.Portal.Api.Models;
using RdpRelay.Portal.Api.Services;

namespace RdpRelay.Portal.Api.Controllers;

[ApiController]
[Route("api/tenants/{tenantId}/[controller]")]
[Authorize]
public class AgentsController : ControllerBase
{
    private readonly AgentService _agentService;
    private readonly TenantService _tenantService;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AgentsController> _logger;

    public AgentsController(AgentService agentService, TenantService tenantService, IJwtService jwtService, ILogger<AgentsController> logger)
    {
        _agentService = agentService;
        _tenantService = tenantService;
        _jwtService = jwtService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<Agent>>> GetAgents(
        string tenantId,
        [FromQuery] string? groupId = null,
        [FromQuery] AgentStatus? status = null,
        [FromQuery] string? search = null,
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

            var filter = new AgentFilter
            {
                GroupId = groupId,
                Status = status,
                Search = search
            };

            var agents = await _agentService.GetAgentsAsync(tenantId, filter, skip, limit);
            var total = agents.Count; // In production, get actual count

            return Ok(new PagedResponse<Agent>
            {
                Items = agents,
                Total = total,
                Skip = skip,
                Limit = limit
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving agents for tenant {TenantId}", tenantId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{agentId}")]
    public async Task<ActionResult<Agent>> GetAgent(string tenantId, string agentId)
    {
        try
        {
            // Validate tenant access
            if (!await _tenantService.ValidateTenantAccessAsync(User, tenantId))
            {
                return Forbid();
            }

            var agent = await _agentService.GetAgentAsync(tenantId, agentId);
            if (agent == null)
            {
                return NotFound();
            }

            return Ok(agent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving agent {AgentId} for tenant {TenantId}", agentId, tenantId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost]
    [Authorize(Roles = "SystemAdmin,TenantAdmin,Operator")]
    public async Task<ActionResult<Agent>> CreateAgent(string tenantId, [FromBody] CreateAgentRequest request)
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

            var agent = await _agentService.CreateAgentAsync(tenantId, request);
            return CreatedAtAction(nameof(GetAgent), new { tenantId, agentId = agent.Id }, agent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating agent {AgentName} for tenant {TenantId}", request.Name, tenantId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("{agentId}")]
    [Authorize(Roles = "SystemAdmin,TenantAdmin,Operator")]
    public async Task<ActionResult<Agent>> UpdateAgent(string tenantId, string agentId, [FromBody] UpdateAgentRequest request)
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

            var agent = await _agentService.UpdateAgentAsync(tenantId, agentId, request);
            if (agent == null)
            {
                return NotFound();
            }

            return Ok(agent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating agent {AgentId} for tenant {TenantId}", agentId, tenantId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpDelete("{agentId}")]
    [Authorize(Roles = "SystemAdmin,TenantAdmin,Operator")]
    public async Task<ActionResult> DeleteAgent(string tenantId, string agentId)
    {
        try
        {
            // Validate tenant access
            if (!await _tenantService.ValidateTenantAccessAsync(User, tenantId))
            {
                return Forbid();
            }

            var success = await _agentService.DeleteAgentAsync(tenantId, agentId);
            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting agent {AgentId} for tenant {TenantId}", agentId, tenantId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("stats")]
    public async Task<ActionResult<AgentStats>> GetAgentStats(string tenantId)
    {
        try
        {
            // Validate tenant access
            if (!await _tenantService.ValidateTenantAccessAsync(User, tenantId))
            {
                return Forbid();
            }

            var stats = await _agentService.GetAgentStatsAsync(tenantId);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving agent stats for tenant {TenantId}", tenantId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("online")]
    public async Task<ActionResult<List<Agent>>> GetOnlineAgents(string tenantId)
    {
        try
        {
            // Validate tenant access
            if (!await _tenantService.ValidateTenantAccessAsync(User, tenantId))
            {
                return Forbid();
            }

            var agents = await _agentService.GetOnlineAgentsAsync(tenantId);
            return Ok(agents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving online agents for tenant {TenantId}", tenantId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("provisioning-token")]
    [Authorize(Roles = "SystemAdmin,TenantAdmin,Operator")]
    public async Task<ActionResult<object>> GenerateProvisioningToken(string tenantId, [FromBody] GenerateProvisioningTokenRequest request)
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

            // Generate a unique agent ID for provisioning
            var agentId = ObjectId.GenerateNewId().ToString();
            
            var token = _jwtService.GenerateProvisioningToken(agentId, tenantId, request.GroupId);

            return Ok(new
            {
                token = token,
                agentId = agentId,
                expiresAt = DateTime.UtcNow.AddDays(365)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating provisioning token for tenant {TenantId}", tenantId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
