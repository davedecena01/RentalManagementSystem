namespace RentalManagementApi.Options;

public class SendGridOptions
{
    public const string Section = "SendGrid";

    public string ApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
}
