namespace RentalManagementApi.Entities;

public class TenantProfile
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? IdDocumentUrl { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
