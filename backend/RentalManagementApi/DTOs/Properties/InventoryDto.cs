namespace RentalManagementApi.DTOs.Properties;

public class InventoryItemDto
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Condition { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateInventoryItemRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Condition { get; set; } = "Good";
}

public class UpdateInventoryItemRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Condition { get; set; }
}
