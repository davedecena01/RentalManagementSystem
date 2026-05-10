namespace RentalManagementApi.Entities;

public class ReminderSetting
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LeaseId { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int DaysBeforeDue { get; set; } = 3;
    public int DaysAfterDue { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Lease Lease { get; set; } = null!;
}
