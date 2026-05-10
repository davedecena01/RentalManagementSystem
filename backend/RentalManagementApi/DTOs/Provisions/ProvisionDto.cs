namespace RentalManagementApi.DTOs.Provisions;

public class ProvisionTemplateDto
{
    public Guid Id { get; set; }
    public Guid LandlordId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class LeaseProvisionDto
{
    public Guid Id { get; set; }
    public Guid LeaseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateProvisionTemplateRequest
{
    public required string Title { get; set; }
    public required string Body { get; set; }
}

public class UpdateProvisionTemplateRequest
{
    public required string Title { get; set; }
    public required string Body { get; set; }
}

public class LeaseProvisionPayload
{
    public required string Title { get; set; }
    public required string Body { get; set; }
    public int SortOrder { get; set; }
}
