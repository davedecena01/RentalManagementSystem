namespace RentalManagementApi.Entities;

public enum LeaseStatus { Active, Terminated, Expired }

public class Lease
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PropertyId { get; set; }
    public Guid TenantId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal MonthlyRent { get; set; }
    public decimal DepositAmount { get; set; }
    public decimal AdvanceAmount { get; set; }
    public LeaseStatus Status { get; set; } = LeaseStatus.Active;
    public string? TenantIdFileUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Property Property { get; set; } = null!;
    public User Tenant { get; set; } = null!;
    public ICollection<Payment> Payments { get; set; } = [];
    public ICollection<LeaseProvision> Provisions { get; set; } = [];
}
