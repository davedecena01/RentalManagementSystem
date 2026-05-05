namespace RentalManagementApi.Entities;

public class TenantInvite
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public Guid InvitedByUserId { get; set; }
    public bool IsAccepted { get; set; } = false;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User InvitedBy { get; set; } = null!;
}
