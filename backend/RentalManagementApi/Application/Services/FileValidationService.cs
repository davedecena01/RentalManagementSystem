using Microsoft.Extensions.Options;
using RentalManagementApi.Options;
using System.Net.Http.Headers;

namespace RentalManagementApi.Application.Services;

public class FileValidationService(IOptions<SupabaseOptions> supabaseOptions, IHttpClientFactory httpClientFactory)
{
    private const int MagicByteCount = 12;

    public async Task<bool> ValidateAndDeleteIfInvalidAsync(string bucket, string path)
    {
        var bytes = await DownloadFirstBytesAsync(bucket, path);
        if (bytes is null) return false;   // download failed — do not delete; treat as unverified
        if (!HasValidMagicBytes(bytes))
        {
            await DeleteFileAsync(bucket, path);
            return false;
        }
        return true;
    }

    public static bool HasValidMagicBytes(byte[] bytes)
    {
        if (bytes.Length < MagicByteCount) return false;

        // JPEG: FF D8 FF
        if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF) return true;

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47 &&
            bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A) return true;

        // WebP: RIFF....WEBP (bytes 0-3 = RIFF, bytes 8-11 = WEBP)
        if (bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46 &&
            bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50) return true;

        // PDF: %PDF
        if (bytes[0] == 0x25 && bytes[1] == 0x50 && bytes[2] == 0x44 && bytes[3] == 0x46) return true;

        return false;
    }

    private async Task<byte[]?> DownloadFirstBytesAsync(string bucket, string path)
    {
        try
        {
            var opts = supabaseOptions.Value;
            var client = httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"{opts.Url}/storage/v1/object/{bucket}/{path}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", opts.ServiceRoleKey);
            request.Headers.Add("apikey", opts.ServiceRoleKey);
            request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(0, MagicByteCount - 1);

            var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;

            return await response.Content.ReadAsByteArrayAsync();
        }
        catch
        {
            return null;
        }
    }

    private async Task DeleteFileAsync(string bucket, string path)
    {
        try
        {
            var opts = supabaseOptions.Value;
            var client = httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Delete,
                $"{opts.Url}/storage/v1/object/{bucket}/{path}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", opts.ServiceRoleKey);
            request.Headers.Add("apikey", opts.ServiceRoleKey);
            await client.SendAsync(request);
        }
        catch { /* best-effort delete */ }
    }
}
