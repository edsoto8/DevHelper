namespace DevHelper.Web.Services;

/// <summary>Well-known application setting keys.</summary>
public static class SettingKeys
{
    public const string TicketLookupSqlServerConnectionString = "TicketLookupSqlServerConnectionString";
    public const string ScreenshotSourceDirectory = "ScreenshotSourceDirectory";
    public const string DefaultEnvironment = "DefaultEnvironment";
    public const string LogLevel = "LogLevel";

    /// <summary>Keys whose values must be encrypted at rest.</summary>
    public static readonly IReadOnlySet<string> Sensitive = new HashSet<string>(StringComparer.Ordinal)
    {
        TicketLookupSqlServerConnectionString,
    };
}

/// <summary>
/// Reads and writes application settings, transparently encrypting sensitive keys
/// with ASP.NET Core Data Protection. Decryption happens only through this service.
/// </summary>
public interface IApplicationSettingsService
{
    Task<string?> GetAsync(string key);
    Task SetAsync(string key, string? value);

    Task<string?> GetScreenshotSourceDirectoryAsync();
    Task<string?> GetDefaultEnvironmentAsync();
    Task<string?> GetTicketLookupConnectionStringAsync();
}
