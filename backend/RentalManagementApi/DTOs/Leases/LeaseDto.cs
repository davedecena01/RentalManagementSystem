namespace RentalManagementApi.DTOs.Leases;

public class LeaseDto
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public string PropertyAddress { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string TenantEmail { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal MonthlyRent { get; set; }
    public decimal DepositAmount { get; set; }
    public decimal AdvanceAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? TenantIdFileUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateLeaseRequest
{
    public Guid PropertyId { get; set; }
    public string TenantEmail { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal MonthlyRent { get; set; }
    public decimal DepositAmount { get; set; }
    public decimal AdvanceAmount { get; set; }
    public string? TenantIdFileUrl { get; set; }
}
