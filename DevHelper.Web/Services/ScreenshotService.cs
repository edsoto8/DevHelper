namespace DevHelper.Web.Services;

/// <inheritdoc />
public sealed class ScreenshotService : IScreenshotService
{
    private readonly IApplicationSettingsService _settings;
    private readonly ILogger<ScreenshotService> _logger;

    public ScreenshotService(IApplicationSettingsService settings, ILogger<ScreenshotService> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public async Task<string?> GetSourceDirectoryAsync()
    {
        var dir = await _settings.GetScreenshotSourceDirectoryAsync();
        return string.IsNullOrWhiteSpace(dir) ? null : Path.GetFullPath(dir);
    }

    public async Task<IReadOnlyList<ScreenshotFile>> ListAvailableAsync()
    {
        var sourceDir = await GetSourceDirectoryAsync();
        if (sourceDir is null || !Directory.Exists(sourceDir))
        {
            _logger.LogWarning("Screenshot source directory is not configured or does not exist.");
            return Array.Empty<ScreenshotFile>();
        }

        var files = new List<ScreenshotFile>();
        foreach (var path in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            if (!IScreenshotService.SupportedExtensions.Contains(Path.GetExtension(path)))
            {
                continue;
            }

            var relative = Path.GetRelativePath(sourceDir, path);
            files.Add(new ScreenshotFile(relative, Path.GetFileName(path), path));
        }

        return files.OrderBy(f => f.RelativePath, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public async Task<ScreenshotValidationResult> ValidateForStorageAsync(string selectedPath)
    {
        var sourceDir = await GetSourceDirectoryAsync();
        if (sourceDir is null)
        {
            return new ScreenshotValidationResult(false, null, "Screenshot source directory is not configured.");
        }

        if (string.IsNullOrWhiteSpace(selectedPath))
        {
            return new ScreenshotValidationResult(false, null, "No file selected.");
        }

        // Resolve relative or absolute input against the source directory.
        var absolute = Path.IsPathRooted(selectedPath)
            ? Path.GetFullPath(selectedPath)
            : Path.GetFullPath(Path.Combine(sourceDir, selectedPath));

        if (!IsInside(sourceDir, absolute))
        {
            _logger.LogWarning("Rejected screenshot path outside the source directory.");
            return new ScreenshotValidationResult(false, null, "File is outside the configured screenshot source directory.");
        }

        if (!IScreenshotService.SupportedExtensions.Contains(Path.GetExtension(absolute)))
        {
            return new ScreenshotValidationResult(false, null, "Unsupported file type. Allowed: .png, .jpg, .jpeg, .webp.");
        }

        if (Directory.Exists(absolute) || !File.Exists(absolute))
        {
            return new ScreenshotValidationResult(false, null, "Selected file does not exist.");
        }

        var relative = Path.GetRelativePath(sourceDir, absolute);
        _logger.LogInformation("Validated screenshot attachment {RelativePath}.", relative);
        return new ScreenshotValidationResult(true, relative, null);
    }

    public async Task<string?> ResolveExistingAsync(string storedPath)
    {
        var sourceDir = await GetSourceDirectoryAsync();
        if (sourceDir is null || string.IsNullOrWhiteSpace(storedPath))
        {
            return null;
        }

        var absolute = Path.IsPathRooted(storedPath)
            ? Path.GetFullPath(storedPath)
            : Path.GetFullPath(Path.Combine(sourceDir, storedPath));

        if (!IsInside(sourceDir, absolute) || !File.Exists(absolute))
        {
            return null;
        }

        return absolute;
    }

    /// <summary>True when <paramref name="candidate"/> resolves inside <paramref name="root"/>.</summary>
    private static bool IsInside(string root, string candidate)
    {
        var normalizedRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        var normalizedCandidate = Path.GetFullPath(candidate);
        return normalizedCandidate.Equals(normalizedRoot, PathComparison)
               || normalizedCandidate.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, PathComparison);
    }

    private static StringComparison PathComparison =>
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
}
