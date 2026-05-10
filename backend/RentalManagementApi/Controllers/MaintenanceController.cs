using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalManagementApi.Application.Services;
using RentalManagementApi.Common;
using RentalManagementApi.DTOs.Maintenance;

namespace RentalManagementApi.Controllers;

[ApiController]
[Route("api/maintenance")]
[Authorize]
public class MaintenanceController(MaintenanceService maintenanceService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        if (userId is null) return Unauthorized(new ApiError("Invalid token.", "UNAUTHORIZED"));

        var requests = role == "Landlord"
            ? await maintenanceService.GetForLandlordAsync(userId.Value)
            : await maintenanceService.GetForTenantAsync(userId.Value);

        return Ok(requests);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        if (userId is null) return Unauthorized(new ApiError("Invalid token.", "UNAUTHORIZED"));

        var request = await maintenanceService.GetByIdAsync(id, userId.Value, role);
        if (request is null) return NotFound(new ApiError("Maintenance request not found.", "NOT_FOUND"));

        return Ok(request);
    }

    [HttpPost]
    [Authorize(Policy = "TenantPolicy")]
    public async Task<IActionResult> Create([FromBody] CreateMaintenanceRequestDto dto)
    {
        var tenantId = GetCurrentUserId();
        if (tenantId is null) return Unauthorized(new ApiError("Invalid token.", "UNAUTHORIZED"));

        var (request, error) = await maintenanceService.CreateAsync(tenantId.Value, dto);

        return error switch
        {
            "PROPERTY_NOT_FOUND" => NotFound(new ApiError("Property not found.", error)),
            "NO_ACTIVE_LEASE" => BadRequest(new ApiError("You do not have an active lease for this property.", error)),
            not null => BadRequest(new ApiError("Could not submit request.", error)),
            _ => CreatedAtAction(nameof(GetById), new { id = request!.Id }, request)
        };
    }

    [HttpPatch("{id:guid}/resolve")]
    [Authorize(Policy = "LandlordPolicy")]
    public async Task<IActionResult> Resolve(Guid id, [FromBody] ResolveMaintenanceRequestDto dto)
    {
        var landlordId = GetCurrentUserId();
        if (landlordId is null) return Unauthorized(new ApiError("Invalid token.", "UNAUTHORIZED"));

        var (request, error) = await maintenanceService.ResolveAsync(id, landlordId.Value, dto);

        return error switch
        {
            "NOT_FOUND" => NotFound(new ApiError("Maintenance request not found.", error)),
            "FORBIDDEN" => Forbid(),
            not null => BadRequest(new ApiError("Could not resolve request.", error)),
            _ => Ok(request)
        };
    }

    private Guid? GetCurrentUserId()
    {
        var sub = User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    private string GetCurrentUserRole() =>
        User.FindFirst("user_role")?.Value ?? string.Empty;
}
