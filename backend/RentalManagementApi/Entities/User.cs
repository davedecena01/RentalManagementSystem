namespace RentalManagementApi.Entities;

public class User
{
    public Guid Id { get; set; }           // mirrors Supabase Auth UID
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Role { get; set; } = "Tenant";  // Landlord | Tenant
    public bool OnboardingCompleted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public TenantProfile? TenantProfile { get; set; }
}
