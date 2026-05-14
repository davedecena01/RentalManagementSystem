using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalManagementApi.Application.Services;
using RentalManagementApi.Common;

namespace RentalManagementApi.Controllers;

[ApiController]
[Route("api/seed")]
[Authorize(Policy = "LandlordPolicy")]
public class SeedController(SeedService seedService) : ControllerBase
{
    [HttpPost("demo")]
    public async Task<IActionResult> SeedDemo()
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized(new ApiError("Invalid token.", "UNAUTHORIZED"));

        var (alreadySeeded, summary) = await seedService.SeedDemoAsync(userId.Value);

        if (alreadySeeded)
            return Ok(new { message = "Demo data already loaded." });

        return Ok(new { message = "Demo data loaded.", summary });
    }

    private Guid? GetCurrentUserId()
    {
        var sub = User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
