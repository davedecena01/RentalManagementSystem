using Microsoft.EntityFrameworkCore;
using RentalManagementApi.Data;
using RentalManagementApi.DTOs.Auth;
using RentalManagementApi.Entities;

namespace RentalManagementApi.Application.Services;

public class AuthService(AppDbContext db, ILogger<AuthService> logger)
{
    public async Task<User> RegisterAsync(Guid supabaseUserId, RegisterRequest request)
    {
        var existing = await db.Users.FindAsync(supabaseUserId);
        if (existing is not null)
            return existing;

        var user = new User
        {
            Id = supabaseUserId,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = "Landlord"
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();
        logger.LogInformation("Registered new landlord: {UserId}", user.Id);
        return user;
    }

    public async Task<TenantInvite?> GetInviteAsync(string token) =>
        await db.TenantInvites.FirstOrDefaultAsync(i => i.Token == token && !i.IsAccepted && i.ExpiresAt > DateTime.UtcNow);

    public async Task<TenantInvite> InviteTenantAsync(Guid landlordId, string email)
    {
        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                          .Replace("+", "-").Replace("/", "_").Replace("=", "");

        var invite = new TenantInvite
        {
            Email = email.ToLowerInvariant(),
            Token = token,
            InvitedByUserId = landlordId,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        db.TenantInvites.Add(invite);
        await db.SaveChangesAsync();
        logger.LogInformation("Tenant invite created for {Email} by landlord {LandlordId}", email, landlordId);
        return invite;
    }

    public async Task<User> AcceptInviteAsync(AcceptInviteRequest request)
    {
        var invite = await db.TenantInvites
            .FirstOrDefaultAsync(i => i.Token == request.Token && !i.IsAccepted);

        if (invite is null)
            throw new InvalidOperationException("INVALID_INVITE_TOKEN");

        if (invite.ExpiresAt < DateTime.UtcNow)
            throw new InvalidOperationException("INVITE_TOKEN_EXPIRED");

        var supabaseId = Guid.Parse(request.SupabaseUserId);

        var user = new User
        {
            Id = supabaseId,
            Email = invite.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = "Tenant"
        };

        var tenantProfile = new TenantProfile
        {
            UserId = supabaseId
        };

        invite.IsAccepted = true;

        db.Users.Add(user);
        db.TenantProfiles.Add(tenantProfile);
        await db.SaveChangesAsync();

        logger.LogInformation("Tenant {UserId} accepted invite {InviteId}", user.Id, invite.Id);
        return user;
    }
}
