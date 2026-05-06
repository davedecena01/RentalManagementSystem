namespace RentalManagementApi.Entities;

public enum ItemCondition { Good, Fair, Poor }

public class PropertyInventory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PropertyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ItemCondition Condition { get; set; } = ItemCondition.Good;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Property Property { get; set; } = null!;
}
