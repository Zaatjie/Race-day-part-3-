using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Api.Services;

/// <summary>Stores uploaded images in Azure Blob Storage. Only the API talks to Azure.</summary>
public interface IBlobStorageService
{
    /// <summary>Uploads an image into the given folder (e.g. "banners") and returns its public URL.</summary>
    Task<string> UploadImageAsync(IFormFile file, string folder, CancellationToken ct = default);
}

public class BlobStorageService : IBlobStorageService
{
    private static readonly Dictionary<string, string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp"
    };

    public const long MaxBytes = 5 * 1024 * 1024; // 5 MB

    private readonly IConfiguration _config;
    private BlobContainerClient? _container;

    public BlobStorageService(IConfiguration config) => _config = config;

    /// <summary>Returns an error message if the file is not an acceptable image, otherwise null.</summary>
    public static string? Validate(IFormFile? file)
    {
        if (file is null || file.Length == 0) return "Please choose an image file to upload.";
        if (file.Length > MaxBytes) return "The image must be 5 MB or smaller.";
        if (!AllowedTypes.ContainsKey(file.ContentType)) return "Only JPG, PNG or WEBP images are allowed.";
        return null;
    }

    public async Task<string> UploadImageAsync(IFormFile file, string folder, CancellationToken ct = default)
    {
        var container = await GetContainerAsync(ct);
        var ext = AllowedTypes[file.ContentType];
        var blob = container.GetBlobClient($"{folder}/{Guid.NewGuid():N}{ext}");

        await using var stream = file.OpenReadStream();
        await blob.UploadAsync(stream, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType }
        }, ct);
        return blob.Uri.ToString();
    }

    private async Task<BlobContainerClient> GetContainerAsync(CancellationToken ct)
    {
        if (_container is not null) return _container;

        var conn = _config["AzureBlob:ConnectionString"];
        if (string.IsNullOrWhiteSpace(conn))
            throw new InvalidOperationException("AzureBlob:ConnectionString is not configured.");
        var name = _config["AzureBlob:ContainerName"] ?? "raceday-images";

        var client = new BlobContainerClient(conn, name);
        // Blob-level public read so the browser can show the images by URL.
        await client.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: ct);
        _container = client;
        return client;
    }
}
