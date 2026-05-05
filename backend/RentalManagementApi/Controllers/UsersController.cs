using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalManagementApi.Application.Services;
using RentalManagementApi.Common;
using RentalManagementApi.DTOs.Users;

namespace RentalManagementApi.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController(UserService userService) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new ApiError("Invalid token.", "UNAUTHORIZED"));

        var user = await userService.GetByIdAsync(userId.Value);
        if (user is null)
            return NotFound(new ApiError("User profile not found.", "USER_NOT_FOUND"));

        return Ok(UserService.ToDto(user));
    }

    [HttpPatch("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new ApiError("Invalid token.", "UNAUTHORIZED"));

        var user = await userService.UpdateProfileAsync(userId.Value, request);
        if (user is null)
            return NotFound(new ApiError("User profile not found.", "USER_NOT_FOUND"));

        return Ok(UserService.ToDto(user));
    }

    private Guid? GetCurrentUserId()
    {
        var sub = User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
