namespace DevHelper.Web;

/// <summary>
/// Strongly-typed application options bound from the "DevHelper" configuration section.
/// </summary>
public sealed class DevHelperOptions
{
    public const string SectionName = "DevHelper";

    /// <summary>Path to the SQLite database file, relative to the content root or absolute.</summary>
    public string DatabasePath { get; set; } = "Data/devhelper.db";

    /// <summary>
    /// Fallback screenshot source directory used only when no value is stored in
    /// ApplicationSettings. The stored setting always takes precedence.
    /// </summary>
    public string ScreenshotSourceDirectory { get; set; } = string.Empty;
}
