using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RentalManagementApi.Application.Services;
using RentalManagementApi.Data;
using RentalManagementApi.DTOs.Auth;

namespace RentalManagementApi.Tests;

public class AuthServiceTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task RegisterAsync_CreatesLandlord_WithCorrectRole()
    {
        using var db = CreateDb();
        var service = new AuthService(db, NullLogger<AuthService>.Instance);
        var id = Guid.NewGuid();

        var user = await service.RegisterAsync(id, new RegisterRequest
        {
            FirstName = "Dave",
            LastName = "Cruz",
            Email = "dave@example.com"
        });

        Assert.Equal("Landlord", user.Role);
        Assert.Equal(id, user.Id);
    }

    [Fact]
    public async Task RegisterAsync_IsIdempotent_WhenCalledTwice()
    {
        using var db = CreateDb();
        var service = new AuthService(db, NullLogger<AuthService>.Instance);
        var id = Guid.NewGuid();
        var request = new RegisterRequest { FirstName = "Dave", LastName = "Cruz", Email = "dave@example.com" };

        var first = await service.RegisterAsync(id, request);
        var second = await service.RegisterAsync(id, request);

        Assert.Equal(first.Id, second.Id);
        Assert.Single(db.Users);
    }

    [Fact]
    public async Task InviteTenantAsync_CreatesInvite_WithFutureExpiry()
    {
        using var db = CreateDb();
        var service = new AuthService(db, NullLogger<AuthService>.Instance);

        var landlordId = Guid.NewGuid();
        db.Users.Add(new Entities.User { Id = landlordId, Email = "landlord@example.com", FirstName = "L", LastName = "L", Role = "Landlord" });
        await db.SaveChangesAsync();

        var invite = await service.InviteTenantAsync(landlordId, "tenant@example.com");

        Assert.NotEmpty(invite.Token);
        Assert.True(invite.ExpiresAt > DateTime.UtcNow);
        Assert.False(invite.IsAccepted);
    }

    [Fact]
    public async Task AcceptInviteAsync_CreatesTenanWithProfile()
    {
        using var db = CreateDb();
        var service = new AuthService(db, NullLogger<AuthService>.Instance);

        var landlordId = Guid.NewGuid();
        db.Users.Add(new Entities.User { Id = landlordId, Email = "landlord@example.com", FirstName = "L", LastName = "L", Role = "Landlord" });
        var invite = await service.InviteTenantAsync(landlordId, "tenant@example.com");

        var tenantId = Guid.NewGuid();
        var user = await service.AcceptInviteAsync(new AcceptInviteRequest
        {
            Token = invite.Token,
            FirstName = "Maria",
            LastName = "Santos",
            SupabaseUserId = tenantId.ToString()
        });

        Assert.Equal("Tenant", user.Role);
        Assert.True((await db.TenantInvites.FindAsync(invite.Id))!.IsAccepted);
        Assert.NotNull(await db.TenantProfiles.FirstOrDefaultAsync(p => p.UserId == tenantId));
    }

    [Fact]
    public async Task AcceptInviteAsync_ThrowsOnExpiredToken()
    {
        using var db = CreateDb();
        var service = new AuthService(db, NullLogger<AuthService>.Instance);

        var landlordId = Guid.NewGuid();
        db.Users.Add(new Entities.User { Id = landlordId, Email = "landlord@example.com", FirstName = "L", LastName = "L", Role = "Landlord" });
        await db.SaveChangesAsync();

        var expiredInvite = new Entities.TenantInvite
        {
            Email = "expired@example.com",
            Token = "expiredtoken",
            InvitedByUserId = landlordId,
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };
        db.TenantInvites.Add(expiredInvite);
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AcceptInviteAsync(new AcceptInviteRequest
            {
                Token = "expiredtoken",
                FirstName = "X",
                LastName = "Y",
                SupabaseUserId = Guid.NewGuid().ToString()
            }));

        Assert.Equal("INVITE_TOKEN_EXPIRED", ex.Message);
    }
}
