using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RentalManagementApi.Application.Services;
using RentalManagementApi.Common;
using RentalManagementApi.Options;

namespace RentalManagementApi.Controllers;

[ApiController]
[Route("api/seed")]
[Authorize(Policy = "LandlordPolicy")]
public class SeedController(SeedService seedService, IOptions<AppOptions> appOptions) : ControllerBase
{
    private readonly IOptions<AppOptions> appOptions = appOptions;
    [HttpPost("demo")]
    public async Task<IActionResult> SeedDemo()
    {
        if (!appOptions.Value.EnableSeedEndpoint)
            return NotFound(new ApiError("Endpoint not available.", "NOT_FOUND"));

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
