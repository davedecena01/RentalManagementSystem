using Microsoft.EntityFrameworkCore;
using RentalManagementApi.Data;
using RentalManagementApi.DTOs.Provisions;
using RentalManagementApi.Entities;

namespace RentalManagementApi.Application.Services;

public class ProvisionService(AppDbContext db)
{
    // --- Templates ---

    public async Task<List<ProvisionTemplateDto>> GetTemplatesAsync(Guid landlordId)
    {
        return await db.ProvisionTemplates
            .Where(t => t.LandlordId == landlordId)
            .OrderBy(t => t.CreatedAt)
            .Select(t => ToTemplateDto(t))
            .ToListAsync();
    }

    public async Task<ProvisionTemplateDto> CreateTemplateAsync(Guid landlordId, CreateProvisionTemplateRequest request)
    {
        var template = new ProvisionTemplate
        {
            LandlordId = landlordId,
            Title = request.Title.Trim(),
            Body = request.Body.Trim()
        };
        db.ProvisionTemplates.Add(template);
        await db.SaveChangesAsync();
        return ToTemplateDto(template);
    }

    public async Task<ProvisionTemplateDto?> UpdateTemplateAsync(Guid id, Guid landlordId, UpdateProvisionTemplateRequest request)
    {
        var template = await db.ProvisionTemplates
            .FirstOrDefaultAsync(t => t.Id == id && t.LandlordId == landlordId);
        if (template is null) return null;

        template.Title = request.Title.Trim();
        template.Body = request.Body.Trim();
        await db.SaveChangesAsync();
        return ToTemplateDto(template);
    }

    public async Task<bool> DeleteTemplateAsync(Guid id, Guid landlordId)
    {
        var template = await db.ProvisionTemplates
            .FirstOrDefaultAsync(t => t.Id == id && t.LandlordId == landlordId);
        if (template is null) return false;

        db.ProvisionTemplates.Remove(template);
        await db.SaveChangesAsync();
        return true;
    }

    // --- Lease Provisions ---

    public async Task<List<LeaseProvisionDto>> GetLeaseProvisionsAsync(Guid leaseId, Guid userId, string role)
    {
        var hasAccess = role == "Landlord"
            ? await db.Leases.AnyAsync(l => l.Id == leaseId && l.Property.LandlordId == userId)
            : await db.Leases.AnyAsync(l => l.Id == leaseId && l.TenantId == userId);

        if (!hasAccess) throw new UnauthorizedAccessException("Access denied.");

        return await db.LeaseProvisions
            .Where(p => p.LeaseId == leaseId)
            .OrderBy(p => p.SortOrder)
            .Select(p => ToProvisionDto(p))
            .ToListAsync();
    }

    public async Task<List<LeaseProvisionDto>> SetLeaseProvisionsAsync(
        Guid leaseId, Guid landlordId, List<LeaseProvisionPayload> payloads)
    {
        var leaseExists = await db.Leases
            .AnyAsync(l => l.Id == leaseId && l.Property.LandlordId == landlordId);
        if (!leaseExists) throw new KeyNotFoundException("Lease not found.");

        var existing = await db.LeaseProvisions.Where(p => p.LeaseId == leaseId).ToListAsync();
        db.LeaseProvisions.RemoveRange(existing);

        var newProvisions = payloads.Select((p, idx) => new LeaseProvision
        {
            LeaseId = leaseId,
            Title = p.Title.Trim(),
            Body = p.Body.Trim(),
            SortOrder = p.SortOrder != 0 ? p.SortOrder : idx
        }).ToList();

        db.LeaseProvisions.AddRange(newProvisions);
        await db.SaveChangesAsync();

        return newProvisions.Select(ToProvisionDto).ToList();
    }

    public async Task<bool> DeleteLeaseProvisionAsync(Guid leaseId, Guid provisionId, Guid landlordId)
    {
        var provision = await db.LeaseProvisions
            .Include(p => p.Lease).ThenInclude(l => l.Property)
            .FirstOrDefaultAsync(p =>
                p.Id == provisionId &&
                p.LeaseId == leaseId &&
                p.Lease.Property.LandlordId == landlordId);
        if (provision is null) return false;

        db.LeaseProvisions.Remove(provision);
        await db.SaveChangesAsync();
        return true;
    }

    private static ProvisionTemplateDto ToTemplateDto(ProvisionTemplate t) => new()
    {
        Id = t.Id,
        LandlordId = t.LandlordId,
        Title = t.Title,
        Body = t.Body,
        CreatedAt = t.CreatedAt
    };

    private static LeaseProvisionDto ToProvisionDto(LeaseProvision p) => new()
    {
        Id = p.Id,
        LeaseId = p.LeaseId,
        Title = p.Title,
        Body = p.Body,
        SortOrder = p.SortOrder,
        CreatedAt = p.CreatedAt
    };
}
