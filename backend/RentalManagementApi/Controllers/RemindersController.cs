using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RentalManagementApi.Application.Services;
using RentalManagementApi.Common;
using RentalManagementApi.DTOs.Reminders;
using RentalManagementApi.Options;

namespace RentalManagementApi.Controllers;

[ApiController]
[Route("api/reminders")]
public class RemindersController(ReminderService reminderService, IOptions<AppOptions> appOptions) : ControllerBase
{
    [HttpGet("{leaseId:guid}")]
    [Authorize(Policy = "LandlordPolicy")]
    public async Task<IActionResult> Get(Guid leaseId)
    {
        var setting = await reminderService.GetOrCreateAsync(leaseId);
        return Ok(setting);
    }

    [HttpPut("{leaseId:guid}")]
    [Authorize(Policy = "LandlordPolicy")]
    public async Task<IActionResult> Update(Guid leaseId, [FromBody] UpdateReminderSettingRequest request)
    {
        var landlordId = GetCurrentUserId();
        if (landlordId is null) return Unauthorized(new ApiError("Invalid token.", "UNAUTHORIZED"));

        var setting = await reminderService.UpdateAsync(leaseId, landlordId.Value, request);
        if (setting is null) return NotFound(new ApiError("Lease not found.", "LEASE_NOT_FOUND"));

        return Ok(setting);
    }

    [HttpPost("trigger")]
    public async Task<IActionResult> Trigger()
    {
        var key = Request.Headers["X-Internal-Key"].ToString();
        if (key != appOptions.Value.InternalApiKey || string.IsNullOrWhiteSpace(key))
            return Unauthorized(new ApiError("Invalid internal key.", "UNAUTHORIZED"));

        var result = await reminderService.TriggerAsync();
        return Ok(result);
    }

    private Guid? GetCurrentUserId()
    {
        var sub = User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
