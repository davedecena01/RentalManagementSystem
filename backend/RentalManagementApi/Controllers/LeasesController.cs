using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalManagementApi.Application.Services;
using RentalManagementApi.Common;
using RentalManagementApi.DTOs.Leases;

namespace RentalManagementApi.Controllers;

[ApiController]
[Route("api/leases")]
[Authorize]
public class LeasesController(LeaseService leaseService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        if (userId is null) return Unauthorized(new ApiError("Invalid token.", "UNAUTHORIZED"));

        var leases = role == "Landlord"
            ? await leaseService.GetAllForLandlordAsync(userId.Value)
            : await leaseService.GetAllForTenantAsync(userId.Value);

        return Ok(leases);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        if (userId is null) return Unauthorized(new ApiError("Invalid token.", "UNAUTHORIZED"));

        var lease = role == "Landlord"
            ? await leaseService.GetByIdForLandlordAsync(id, userId.Value)
            : await leaseService.GetByIdForTenantAsync(id, userId.Value);

        if (lease is null) return NotFound(new ApiError("Lease not found.", "LEASE_NOT_FOUND"));
        return Ok(lease);
    }

    [HttpPost]
    [Authorize(Policy = "LandlordPolicy")]
    public async Task<IActionResult> Create([FromBody] CreateLeaseRequest request)
    {
        var landlordId = GetCurrentUserId();
        if (landlordId is null) return Unauthorized(new ApiError("Invalid token.", "UNAUTHORIZED"));

        var (lease, error) = await leaseService.CreateAsync(landlordId.Value, request);

        return error switch
        {
            "PROPERTY_NOT_FOUND" => NotFound(new ApiError("Property not found.", error)),
            "PROPERTY_ALREADY_OCCUPIED" => Conflict(new ApiError("Property already has an active lease.", error)),
            "INVALID_DATE_RANGE" => BadRequest(new ApiError("End date must be after start date.", error)),
            "TENANT_NOT_FOUND" => NotFound(new ApiError("No tenant found with that email.", error)),
            not null => BadRequest(new ApiError("Could not create lease.", error)),
            _ => CreatedAtAction(nameof(GetById), new { id = lease!.Id }, lease)
        };
    }

    [HttpPatch("{id:guid}/terminate")]
    [Authorize(Policy = "LandlordPolicy")]
    public async Task<IActionResult> Terminate(Guid id)
    {
        var landlordId = GetCurrentUserId();
        if (landlordId is null) return Unauthorized(new ApiError("Invalid token.", "UNAUTHORIZED"));

        var lease = await leaseService.TerminateAsync(id, landlordId.Value);
        if (lease is null) return NotFound(new ApiError("Lease not found.", "LEASE_NOT_FOUND"));

        return Ok(lease);
    }

    private Guid? GetCurrentUserId()
    {
        var sub = User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    private string GetCurrentUserRole() =>
        User.FindFirst("user_role")?.Value ?? string.Empty;
}
