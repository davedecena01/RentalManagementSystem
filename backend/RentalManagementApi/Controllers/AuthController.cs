using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalManagementApi.Application.Services;
using RentalManagementApi.Common;
using RentalManagementApi.DTOs.Auth;

namespace RentalManagementApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AuthService authService) : ControllerBase
{
    [HttpPost("register")]
    [Authorize]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var supabaseId = GetCurrentUserId();
        if (supabaseId is null)
            return Unauthorized(new ApiError("Invalid token.", "UNAUTHORIZED"));

        var user = await authService.RegisterAsync(supabaseId.Value, request);
        return Ok(new { id = user.Id, role = user.Role });
    }

    [HttpPost("invite-tenant")]
    [Authorize(Policy = "LandlordPolicy")]
    public async Task<IActionResult> InviteTenant([FromBody] InviteTenantRequest request)
    {
        var landlordId = GetCurrentUserId();
        if (landlordId is null)
            return Unauthorized(new ApiError("Invalid token.", "UNAUTHORIZED"));

        var invite = await authService.InviteTenantAsync(landlordId.Value, request.Email);

        // TODO Wave 3: send invite email via SendGrid
        return Ok(new { message = "Invite created.", token = invite.Token, expiresAt = invite.ExpiresAt });
    }

    [HttpPost("accept-invite")]
    [AllowAnonymous]
    public async Task<IActionResult> AcceptInvite([FromBody] AcceptInviteRequest request)
    {
        try
        {
            var user = await authService.AcceptInviteAsync(request);
            return Ok(new { id = user.Id, role = user.Role });
        }
        catch (InvalidOperationException ex) when (ex.Message == "INVALID_INVITE_TOKEN")
        {
            return BadRequest(new ApiError("Invite token is invalid or already used.", "INVALID_INVITE_TOKEN"));
        }
        catch (InvalidOperationException ex) when (ex.Message == "INVITE_TOKEN_EXPIRED")
        {
            return BadRequest(new ApiError("Invite token has expired.", "INVITE_TOKEN_EXPIRED"));
        }
    }

    private Guid? GetCurrentUserId()
    {
        var sub = User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
