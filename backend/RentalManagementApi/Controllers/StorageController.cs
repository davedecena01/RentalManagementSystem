using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RentalManagementApi.Common;
using RentalManagementApi.Options;
using System.Net.Http.Headers;
using System.Text.Json;

namespace RentalManagementApi.Controllers;

[ApiController]
[Route("api/storage")]
[Authorize]
public class StorageController(IOptions<SupabaseOptions> supabaseOptions, IHttpClientFactory httpClientFactory) : ControllerBase
{
    private static readonly HashSet<string> AllowedBuckets = ["payment-proofs", "tenant-ids", "maintenance-images"];

    [HttpGet("upload-url")]
    public async Task<IActionResult> GetUploadUrl([FromQuery] string bucket, [FromQuery] string path)
    {
        if (string.IsNullOrWhiteSpace(bucket) || string.IsNullOrWhiteSpace(path))
            return BadRequest(new ApiError("bucket and path are required.", "INVALID_INPUT"));

        if (!AllowedBuckets.Contains(bucket))
            return BadRequest(new ApiError("Invalid storage bucket.", "INVALID_BUCKET"));

        var opts = supabaseOptions.Value;
        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", opts.ServiceRoleKey);
        client.DefaultRequestHeaders.Add("apikey", opts.ServiceRoleKey);

        var body = JsonSerializer.Serialize(new { expiresIn = 3600 });
        var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");

        var response = await client.PostAsync(
            $"{opts.Url}/storage/v1/object/upload/sign/{bucket}/{path}",
            content);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, new ApiError("Failed to get upload URL.", "STORAGE_ERROR"));
        }

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var signedUrl = doc.RootElement.GetProperty("url").GetString();

        return Ok(new { uploadUrl = $"{opts.Url}/storage/v1{signedUrl}" });
    }
}
