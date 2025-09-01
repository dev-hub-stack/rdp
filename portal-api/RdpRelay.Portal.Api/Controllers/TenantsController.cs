using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RdpRelay.Portal.Api.Models;
using RdpRelay.Portal.Api.Services;
using System.Security.Claims;

namespace RdpRelay.Portal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TenantsController : ControllerBase
{
    private readonly TenantService _tenantService;
    private readonly ILogger<TenantsController> _logger;

    public TenantsController(TenantService tenantService, ILogger<TenantsController> logger)
    {
        _tenantService = tenantService;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult<PagedResponse<Tenant>>> GetTenants([FromQuery] int skip = 0, [FromQuery] int limit = 50)
    {
        try
        {
            if (limit > 100) limit = 100; // Cap the limit

            var tenants = await _tenantService.GetTenantsAsync(skip, limit);
            var total = tenants.Count; // In production, get actual count from database

            return Ok(new PagedResponse<Tenant>
            {
                Items = tenants,
                Total = total,
                Skip = skip,
                Limit = limit
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tenants");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{tenantId}")]
    public async Task<ActionResult<Tenant>> GetTenant(string tenantId)
    {
        try
        {
            // Check if user has access to this tenant
            var userTenantId = _tenantService.GetTenantIdFromClaims(User);
            var isSystemAdmin = User.IsInRole("SystemAdmin");

            if (!isSystemAdmin && userTenantId != tenantId)
            {
                return Forbid();
            }

            var tenant = await _tenantService.GetTenantAsync(tenantId);
            if (tenant == null)
            {
                return NotFound();
            }

            return Ok(tenant);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tenant {TenantId}", tenantId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult<Tenant>> CreateTenant([FromBody] CreateTenantRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var tenant = await _tenantService.CreateTenantAsync(request);
            return CreatedAtAction(nameof(GetTenant), new { tenantId = tenant.Id }, tenant);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tenant {TenantName}", request.Name);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("{tenantId}")]
    public async Task<ActionResult<Tenant>> UpdateTenant(string tenantId, [FromBody] UpdateTenantRequest request)
    {
        try
        {
            // Check if user has access to this tenant
            var userTenantId = _tenantService.GetTenantIdFromClaims(User);
            var isSystemAdmin = User.IsInRole("SystemAdmin");
            var isTenantAdmin = User.IsInRole("TenantAdmin");

            if (!isSystemAdmin && (userTenantId != tenantId || !isTenantAdmin))
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var tenant = await _tenantService.UpdateTenantAsync(tenantId, request);
            if (tenant == null)
            {
                return NotFound();
            }

            return Ok(tenant);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tenant {TenantId}", tenantId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpDelete("{tenantId}")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult> DeleteTenant(string tenantId)
    {
        try
        {
            var success = await _tenantService.DeleteTenantAsync(tenantId);
            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting tenant {TenantId}", tenantId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
