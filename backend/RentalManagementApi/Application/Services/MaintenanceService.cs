using Microsoft.EntityFrameworkCore;
using RentalManagementApi.Data;
using RentalManagementApi.DTOs.Maintenance;
using RentalManagementApi.Entities;

namespace RentalManagementApi.Application.Services;

public class MaintenanceService(AppDbContext db)
{
    public async Task<List<MaintenanceRequestDto>> GetForLandlordAsync(Guid landlordId)
    {
        return await db.MaintenanceRequests
            .Include(m => m.Property)
            .Include(m => m.Tenant)
            .Where(m => m.Property.LandlordId == landlordId)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => ToDto(m))
            .ToListAsync();
    }

    public async Task<List<MaintenanceRequestDto>> GetForTenantAsync(Guid tenantId)
    {
        return await db.MaintenanceRequests
            .Include(m => m.Property)
            .Include(m => m.Tenant)
            .Where(m => m.TenantId == tenantId)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => ToDto(m))
            .ToListAsync();
    }

    public async Task<MaintenanceRequestDto?> GetByIdAsync(Guid id, Guid userId, string role)
    {
        var m = await db.MaintenanceRequests
            .Include(m => m.Property)
            .Include(m => m.Tenant)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (m is null) return null;
        if (role == "Landlord" && m.Property.LandlordId != userId) return null;
        if (role == "Tenant" && m.TenantId != userId) return null;

        return ToDto(m);
    }

    public async Task<(MaintenanceRequestDto? Request, string? Error)> CreateAsync(Guid tenantId, CreateMaintenanceRequestDto dto)
    {
        var property = await db.Properties.FindAsync(dto.PropertyId);
        if (property is null) return (null, "PROPERTY_NOT_FOUND");

        var hasActiveLease = await db.Leases.AnyAsync(l =>
            l.PropertyId == dto.PropertyId &&
            l.TenantId == tenantId &&
            l.Status == LeaseStatus.Active);
        if (!hasActiveLease) return (null, "NO_ACTIVE_LEASE");

        if (!Enum.TryParse<MaintenancePriority>(dto.Priority, true, out var priority))
            priority = MaintenancePriority.Medium;

        var request = new MaintenanceRequest
        {
            PropertyId = dto.PropertyId,
            TenantId = tenantId,
            Title = dto.Title,
            Description = dto.Description,
            Priority = priority,
            ImageUrl = dto.ImageUrl
        };

        db.MaintenanceRequests.Add(request);
        await db.SaveChangesAsync();

        await db.Entry(request).Reference(m => m.Property).LoadAsync();
        await db.Entry(request).Reference(m => m.Tenant).LoadAsync();

        return (ToDto(request), null);
    }

    public async Task<(MaintenanceRequestDto? Request, string? Error)> ResolveAsync(Guid id, Guid landlordId, ResolveMaintenanceRequestDto dto)
    {
        var m = await db.MaintenanceRequests
            .Include(m => m.Property)
            .Include(m => m.Tenant)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (m is null) return (null, "NOT_FOUND");
        if (m.Property.LandlordId != landlordId) return (null, "FORBIDDEN");

        m.Status = MaintenanceStatus.Resolved;
        m.ResolutionNotes = dto.ResolutionNotes;
        m.ResolvedAt = DateTime.UtcNow;
        m.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return (ToDto(m), null);
    }

    private static MaintenanceRequestDto ToDto(MaintenanceRequest m) => new()
    {
        Id = m.Id,
        PropertyId = m.PropertyId,
        PropertyName = m.Property.Name,
        TenantId = m.TenantId,
        TenantName = $"{m.Tenant.FirstName} {m.Tenant.LastName}",
        Title = m.Title,
        Description = m.Description,
        Priority = m.Priority.ToString(),
        Status = m.Status.ToString(),
        ImageUrl = m.ImageUrl,
        ResolutionNotes = m.ResolutionNotes,
        ResolvedAt = m.ResolvedAt,
        CreatedAt = m.CreatedAt,
        UpdatedAt = m.UpdatedAt
    };
}
