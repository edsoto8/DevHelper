namespace ReleasePlanGenerator.Web.Services;

public interface IApplicationSettingsService
{
    Task<string?> GetAsync(string key);
    Task SetAsync(string key, string? value, bool isEncrypted = false);
    Task<int?> GetDefaultTemplateIdAsync();
    Task<string?> GetExternalSqlServerConnectionStringAsync();
    Task SetExternalSqlServerConnectionStringAsync(string? connectionString);
}
