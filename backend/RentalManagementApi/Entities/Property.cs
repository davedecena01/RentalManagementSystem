namespace RentalManagementApi.Entities;

public enum PropertyType { Apartment, House, Condo, Commercial }

public class Property
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LandlordId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public PropertyType Type { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User Landlord { get; set; } = null!;
    public ICollection<PropertyInventory> Inventory { get; set; } = [];
    public ICollection<Lease> Leases { get; set; } = [];
}
