using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using RentalManagementApi.Data;

namespace RentalManagementApi.Middleware;

/// <summary>
/// Reads the user's role from the DB and adds it as a "user_role" claim
/// so LandlordPolicy and TenantPolicy work without a Supabase JWT hook.
/// </summary>
public class RoleEnrichmentMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var sub = context.User.FindFirst("sub")?.Value;
            var existingRole = context.User.FindFirst("user_role")?.Value;

            if (sub != null && existingRole == null && Guid.TryParse(sub, out var userId))
            {
                var user = await db.Users.AsNoTracking()
                    .Where(u => u.Id == userId)
                    .Select(u => new { u.Role })
                    .FirstOrDefaultAsync();

                if (user != null)
                {
                    var identity = (ClaimsIdentity)context.User.Identity;
                    identity.AddClaim(new Claim("user_role", user.Role));
                }
            }
        }

        await next(context);
    }
}
