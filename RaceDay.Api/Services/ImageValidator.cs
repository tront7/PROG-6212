namespace RaceDay.Api.Services;

/// <summary>Server-side checks for uploaded images (type and size).</summary>
public static class ImageValidator
{
    public const long MaxBytes = 5 * 1024 * 1024; // 5 MB

    private static readonly string[] AllowedContentTypes = { "image/jpeg", "image/png", "image/webp", "image/gif" };
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

    /// <summary>Returns an error message, or null when the file is acceptable.</summary>
    public static string? Validate(IFormFile? file)
    {
        if (file is null || file.Length == 0)
            return "Please choose an image file to upload.";

        if (file.Length > MaxBytes)
            return "The image must be 5 MB or smaller.";

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var contentType = (file.ContentType ?? string.Empty).ToLowerInvariant();

        if (!AllowedExtensions.Contains(extension) || !AllowedContentTypes.Contains(contentType))
            return "Only JPG, PNG, WEBP or GIF images are allowed.";

        return null;
    }
}
