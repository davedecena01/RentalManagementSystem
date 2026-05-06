namespace RentalManagementApi.DTOs.Properties;

public class PropertyDto
{
    public Guid Id { get; set; }
    public Guid LandlordId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsOccupied { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreatePropertyRequest
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdatePropertyRequest
{
    public string? Name { get; set; }
    public string? Address { get; set; }
    public string? Type { get; set; }
    public string? Description { get; set; }
}
