using Microsoft.EntityFrameworkCore;
using RentalManagementApi.Data;
using RentalManagementApi.DTOs.Leases;
using RentalManagementApi.Entities;

namespace RentalManagementApi.Application.Services;

public class LeaseService(AppDbContext db)
{
    public async Task<List<LeaseDto>> GetAllForLandlordAsync(Guid landlordId)
    {
        return await db.Leases
            .Include(l => l.Property)
            .Include(l => l.Tenant)
            .Where(l => l.Property.LandlordId == landlordId)
            .Select(l => ToDto(l))
            .ToListAsync();
    }

    public async Task<LeaseDto?> GetByIdForLandlordAsync(Guid id, Guid landlordId)
    {
        var lease = await db.Leases
            .Include(l => l.Property)
            .Include(l => l.Tenant)
            .FirstOrDefaultAsync(l => l.Id == id && l.Property.LandlordId == landlordId);
        return lease is null ? null : ToDto(lease);
    }

    public async Task<LeaseDto?> GetByIdForTenantAsync(Guid id, Guid tenantId)
    {
        var lease = await db.Leases
            .Include(l => l.Property)
            .Include(l => l.Tenant)
            .FirstOrDefaultAsync(l => l.Id == id && l.TenantId == tenantId);
        return lease is null ? null : ToDto(lease);
    }

    public async Task<List<LeaseDto>> GetAllForTenantAsync(Guid tenantId)
    {
        return await db.Leases
            .Include(l => l.Property)
            .Include(l => l.Tenant)
            .Where(l => l.TenantId == tenantId)
            .Select(l => ToDto(l))
            .ToListAsync();
    }

    public async Task<(LeaseDto? Lease, string? Error)> CreateAsync(Guid landlordId, CreateLeaseRequest request)
    {
        var property = await db.Properties
            .FirstOrDefaultAsync(p => p.Id == request.PropertyId && p.LandlordId == landlordId);
        if (property is null)
            return (null, "PROPERTY_NOT_FOUND");

        var alreadyOccupied = await db.Leases.AnyAsync(l =>
            l.PropertyId == request.PropertyId && l.Status == LeaseStatus.Active);
        if (alreadyOccupied)
            return (null, "PROPERTY_ALREADY_OCCUPIED");

        if (request.EndDate <= request.StartDate)
            return (null, "INVALID_DATE_RANGE");

        var tenant = await db.Users.FirstOrDefaultAsync(u => u.Email == request.TenantEmail && u.Role == "Tenant");
        if (tenant is null)
            return (null, "TENANT_NOT_FOUND");

        var lease = new Lease
        {
            PropertyId = request.PropertyId,
            TenantId = tenant.Id,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            MonthlyRent = request.MonthlyRent,
            DepositAmount = request.DepositAmount,
            AdvanceAmount = request.AdvanceAmount,
            Status = LeaseStatus.Active,
            TenantIdFileUrl = request.TenantIdFileUrl
        };

        db.Leases.Add(lease);

        // Auto-generate one Payment per month
        var current = request.StartDate;
        while (current <= request.EndDate)
        {
            db.Payments.Add(new Payment
            {
                LeaseId = lease.Id,
                DueDate = current,
                AmountDue = request.MonthlyRent,
                Status = PaymentStatus.Unpaid
            });
            current = current.AddMonths(1);
        }

        await db.SaveChangesAsync();

        lease.Property = property;
        lease.Tenant = tenant;
        return (ToDto(lease), null);
    }

    public async Task<LeaseDto?> TerminateAsync(Guid id, Guid landlordId)
    {
        var lease = await db.Leases
            .Include(l => l.Property)
            .Include(l => l.Tenant)
            .FirstOrDefaultAsync(l => l.Id == id && l.Property.LandlordId == landlordId);
        if (lease is null) return null;

        lease.Status = LeaseStatus.Terminated;
        lease.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return ToDto(lease);
    }

    private static LeaseDto ToDto(Lease l) => new()
    {
        Id = l.Id,
        PropertyId = l.PropertyId,
        PropertyName = l.Property.Name,
        PropertyAddress = l.Property.Address,
        TenantId = l.TenantId,
        TenantName = $"{l.Tenant.FirstName} {l.Tenant.LastName}",
        TenantEmail = l.Tenant.Email,
        StartDate = l.StartDate,
        EndDate = l.EndDate,
        MonthlyRent = l.MonthlyRent,
        DepositAmount = l.DepositAmount,
        AdvanceAmount = l.AdvanceAmount,
        Status = l.Status.ToString(),
        TenantIdFileUrl = l.TenantIdFileUrl,
        CreatedAt = l.CreatedAt,
        UpdatedAt = l.UpdatedAt
    };
}
