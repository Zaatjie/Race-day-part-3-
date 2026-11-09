namespace RaceDay.Web.Services;

/// <summary>Friendly up-front checks so users get a clear message before anything is uploaded.</summary>
public static class ImageRules
{
    private static readonly string[] Allowed = { "image/jpeg", "image/png", "image/webp" };
    public const long MaxBytes = 5 * 1024 * 1024;

    public static string? Validate(IFormFile? file)
    {
        if (file is null || file.Length == 0) return "Please choose an image to upload.";
        if (file.Length > MaxBytes) return "The image must be 5 MB or smaller.";
        if (!Allowed.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
            return "Only JPG, PNG or WEBP images are allowed.";
        return null;
    }
}
