using Microsoft.EntityFrameworkCore;
using RentalManagementApi.Data;
using RentalManagementApi.DTOs.Users;
using RentalManagementApi.Entities;

namespace RentalManagementApi.Application.Services;

public class UserService(AppDbContext db)
{
    public async Task<User?> GetByIdAsync(Guid id) =>
        await db.Users.FindAsync(id);

    public async Task<User?> UpdateProfileAsync(Guid id, UpdateProfileRequest request)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return null;

        if (request.FirstName is not null) user.FirstName = request.FirstName;
        if (request.LastName is not null) user.LastName = request.LastName;
        if (request.Phone is not null) user.Phone = request.Phone;
        if (request.OnboardingCompleted is not null) user.OnboardingCompleted = request.OnboardingCompleted.Value;

        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return user;
    }

    public static UserDto ToDto(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Phone = user.Phone,
        Role = user.Role,
        OnboardingCompleted = user.OnboardingCompleted,
        CreatedAt = user.CreatedAt
    };
}
