namespace RentalManagementApi.Entities;

public enum PaymentStatus { Unpaid, Partial, Paid }

public class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LeaseId { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal AmountDue { get; set; }
    public decimal AmountPaid { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Unpaid;
    public DateTime? PaidAt { get; set; }
    public string? ProofFileUrl { get; set; }
    public string? StripeSessionId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Lease Lease { get; set; } = null!;
}
