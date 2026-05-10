using Microsoft.EntityFrameworkCore;
using RentalManagementApi.Data;
using RentalManagementApi.DTOs.Properties;
using RentalManagementApi.Entities;

namespace RentalManagementApi.Application.Services;

public class PropertyService(AppDbContext db)
{
    public async Task<List<PropertyDto>> GetAllAsync(Guid landlordId)
    {
        return await db.Properties
            .Where(p => p.LandlordId == landlordId)
            .Select(p => ToDto(p, db.Leases.Any(l => l.PropertyId == p.Id && l.Status == LeaseStatus.Active)))
            .ToListAsync();
    }

    public async Task<PropertyDto?> GetByIdAsync(Guid id, Guid landlordId)
    {
        var p = await db.Properties.FirstOrDefaultAsync(p => p.Id == id && p.LandlordId == landlordId);
        if (p is null) return null;
        var occupied = await db.Leases.AnyAsync(l => l.PropertyId == id && l.Status == LeaseStatus.Active);
        return ToDto(p, occupied);
    }

    public async Task<PropertyDto> CreateAsync(Guid landlordId, CreatePropertyRequest request)
    {
        if (!Enum.TryParse<PropertyType>(request.Type, true, out var type))
            throw new ArgumentException("Invalid property type.");

        var property = new Property
        {
            LandlordId = landlordId,
            Name = request.Name,
            Address = request.Address,
            Type = type,
            Description = request.Description
        };

        db.Properties.Add(property);
        await db.SaveChangesAsync();
        return ToDto(property, false);
    }

    public async Task<PropertyDto?> UpdateAsync(Guid id, Guid landlordId, UpdatePropertyRequest request)
    {
        var property = await db.Properties.FirstOrDefaultAsync(p => p.Id == id && p.LandlordId == landlordId);
        if (property is null) return null;

        if (request.Name is not null) property.Name = request.Name;
        if (request.Address is not null) property.Address = request.Address;
        if (request.Description is not null) property.Description = request.Description;
        if (request.Type is not null && Enum.TryParse<PropertyType>(request.Type, true, out var type))
            property.Type = type;

        property.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var occupied = await db.Leases.AnyAsync(l => l.PropertyId == id && l.Status == LeaseStatus.Active);
        return ToDto(property, occupied);
    }

    public async Task<(bool Deleted, string? Error)> DeleteAsync(Guid id, Guid landlordId)
    {
        var property = await db.Properties.FirstOrDefaultAsync(p => p.Id == id && p.LandlordId == landlordId);
        if (property is null) return (false, "NOT_FOUND");

        var hasActiveLease = await db.Leases.AnyAsync(l => l.PropertyId == id && l.Status == LeaseStatus.Active);
        if (hasActiveLease) return (false, "ACTIVE_LEASE");

        db.Properties.Remove(property);
        await db.SaveChangesAsync();
        return (true, null);
    }

    // --- Inventory ---

    public async Task<List<InventoryItemDto>> GetInventoryAsync(Guid propertyId, Guid landlordId)
    {
        var owns = await db.Properties.AnyAsync(p => p.Id == propertyId && p.LandlordId == landlordId);
        if (!owns) return [];

        return await db.PropertyInventory
            .Where(i => i.PropertyId == propertyId)
            .Select(i => ToInventoryDto(i))
            .ToListAsync();
    }

    public async Task<InventoryItemDto?> AddInventoryItemAsync(Guid propertyId, Guid landlordId, CreateInventoryItemRequest request)
    {
        var owns = await db.Properties.AnyAsync(p => p.Id == propertyId && p.LandlordId == landlordId);
        if (!owns) return null;

        if (!Enum.TryParse<ItemCondition>(request.Condition, true, out var condition))
            condition = ItemCondition.Good;

        var item = new PropertyInventory
        {
            PropertyId = propertyId,
            Name = request.Name,
            Description = request.Description,
            Condition = condition
        };

        db.PropertyInventory.Add(item);
        await db.SaveChangesAsync();
        return ToInventoryDto(item);
    }

    public async Task<InventoryItemDto?> UpdateInventoryItemAsync(Guid propertyId, Guid itemId, Guid landlordId, UpdateInventoryItemRequest request)
    {
        var item = await db.PropertyInventory
            .Include(i => i.Property)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.PropertyId == propertyId && i.Property.LandlordId == landlordId);
        if (item is null) return null;

        if (request.Name is not null) item.Name = request.Name;
        if (request.Description is not null) item.Description = request.Description;
        if (request.Condition is not null && Enum.TryParse<ItemCondition>(request.Condition, true, out var condition))
            item.Condition = condition;

        await db.SaveChangesAsync();
        return ToInventoryDto(item);
    }

    public async Task<bool> DeleteInventoryItemAsync(Guid propertyId, Guid itemId, Guid landlordId)
    {
        var item = await db.PropertyInventory
            .Include(i => i.Property)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.PropertyId == propertyId && i.Property.LandlordId == landlordId);
        if (item is null) return false;

        db.PropertyInventory.Remove(item);
        await db.SaveChangesAsync();
        return true;
    }

    private static PropertyDto ToDto(Property p, bool isOccupied) => new()
    {
        Id = p.Id,
        LandlordId = p.LandlordId,
        Name = p.Name,
        Address = p.Address,
        Type = p.Type.ToString(),
        Description = p.Description,
        IsOccupied = isOccupied,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt
    };

    private static InventoryItemDto ToInventoryDto(PropertyInventory i) => new()
    {
        Id = i.Id,
        PropertyId = i.PropertyId,
        Name = i.Name,
        Description = i.Description,
        Condition = i.Condition.ToString(),
        CreatedAt = i.CreatedAt
    };
}
