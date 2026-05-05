namespace RentalManagementApi.Options;

public class AppOptions
{
    public const string Section = "App";

    public string FrontendUrl { get; set; } = string.Empty;
    public string InternalApiKey { get; set; } = string.Empty;
    public string PdfStorageBucket { get; set; } = "lease-documents";
    public bool DemoMode { get; set; } = false;
}
