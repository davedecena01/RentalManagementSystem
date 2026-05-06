namespace RentalManagementApi.DTOs.Payments;

public class PaymentDto
{
    public Guid Id { get; set; }
    public Guid LeaseId { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public DateOnly DueDate { get; set; }
    public decimal AmountDue { get; set; }
    public decimal AmountPaid { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }
    public string? ProofFileUrl { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ManualPayRequest
{
    public decimal AmountPaid { get; set; }
    public string? ProofFileUrl { get; set; }
    public string? Notes { get; set; }
}

public class StripeCheckoutResponse
{
    public string CheckoutUrl { get; set; } = string.Empty;
}
