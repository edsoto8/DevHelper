namespace DevHelper.Web.Services;

public sealed record ScreenshotFile(string RelativePath, string FileName, string AbsolutePath);

public sealed record ScreenshotValidationResult(bool IsValid, string? RelativePath, string? Error);

/// <summary>
/// Selects and validates screenshot files from the configured source directory.
/// The service never uploads, copies, renames, moves, captures, or deletes files.
/// </summary>
public interface IScreenshotService
{
    /// <summary>Supported image extensions (lowercase, with leading dot).</summary>
    static readonly IReadOnlySet<string> SupportedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".webp",
    };

    Task<string?> GetSourceDirectoryAsync();

    /// <summary>Lists selectable image files inside the configured source directory.</summary>
    Task<IReadOnlyList<ScreenshotFile>> ListAvailableAsync();

    /// <summary>
    /// Validates a chosen path (relative or absolute), confirming it resolves inside the
    /// source directory, has a supported extension, and exists. Returns the relative path to store.
    /// </summary>
    Task<ScreenshotValidationResult> ValidateForStorageAsync(string selectedPath);

    /// <summary>Resolves a stored path to an absolute path inside the source directory, or null if invalid/missing.</summary>
    Task<string?> ResolveExistingAsync(string storedPath);
}
