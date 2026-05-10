using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalManagementApi.Application.Services;
using RentalManagementApi.Common;
using RentalManagementApi.DTOs.Provisions;

namespace RentalManagementApi.Controllers;

[ApiController]
[Route("api/leases/{leaseId:guid}/provisions")]
[Authorize]
public class LeaseProvisionsController(ProvisionService provisionService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(Guid leaseId)
    {
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        if (userId is null) return Unauthorized(new ApiError("Invalid token.", "UNAUTHORIZED"));

        try
        {
            var provisions = await provisionService.GetLeaseProvisionsAsync(leaseId, userId.Value, role);
            return Ok(provisions);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPut]
    [Authorize(Policy = "LandlordPolicy")]
    public async Task<IActionResult> Set(Guid leaseId, [FromBody] List<LeaseProvisionPayload> payloads)
    {
        if (payloads.Any(p => string.IsNullOrWhiteSpace(p.Title) || string.IsNullOrWhiteSpace(p.Body)))
            return BadRequest(new ApiError("Each provision must have a title and body.", "INVALID_INPUT"));

        var landlordId = GetCurrentUserId();
        if (landlordId is null) return Unauthorized(new ApiError("Invalid token.", "UNAUTHORIZED"));

        try
        {
            var provisions = await provisionService.SetLeaseProvisionsAsync(leaseId, landlordId.Value, payloads);
            return Ok(provisions);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ApiError("Lease not found.", "LEASE_NOT_FOUND"));
        }
    }

    [HttpDelete("{provisionId:guid}")]
    [Authorize(Policy = "LandlordPolicy")]
    public async Task<IActionResult> Delete(Guid leaseId, Guid provisionId)
    {
        var landlordId = GetCurrentUserId();
        if (landlordId is null) return Unauthorized(new ApiError("Invalid token.", "UNAUTHORIZED"));

        var deleted = await provisionService.DeleteLeaseProvisionAsync(leaseId, provisionId, landlordId.Value);
        if (!deleted) return NotFound(new ApiError("Provision not found.", "PROVISION_NOT_FOUND"));

        return NoContent();
    }

    private Guid? GetCurrentUserId()
    {
        var sub = User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    private string GetCurrentUserRole() =>
        User.FindFirst("user_role")?.Value ?? string.Empty;
}
