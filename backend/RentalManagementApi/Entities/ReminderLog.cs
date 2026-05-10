namespace RentalManagementApi.Entities;

public class ReminderLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LeaseId { get; set; }
    public DateOnly DueDate { get; set; }
    public string ReminderType { get; set; } = string.Empty; // "before" | "after"
    public DateOnly SentDate { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Lease Lease { get; set; } = null!;
}
