using System.ComponentModel.DataAnnotations;

namespace RentalManagementApi.DTOs.Users;

public class UpdateProfileRequest
{
    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    [MaxLength(30)]
    public string? Phone { get; set; }

    public bool? OnboardingCompleted { get; set; }
}
