using ReleasePlanGenerator.Web.Models;
using ReleasePlanGenerator.Web.Repositories;

namespace ReleasePlanGenerator.Web.Services;

public class ApplicationSettingsService(IApplicationSettingRepository repository) : IApplicationSettingsService
{
    public async Task<string?> GetAsync(string key)
    {
        var setting = await repository.GetByKeyAsync(key);
        return setting?.SettingValue;
    }

    public Task SetAsync(string key, string? value, bool isEncrypted = false)
        => repository.UpsertAsync(key, value, isEncrypted);

    public async Task<int?> GetDefaultTemplateIdAsync()
    {
        var value = await GetAsync(SettingKeys.DefaultTemplateId);
        return int.TryParse(value, out var id) ? id : null;
    }

    public Task<string?> GetExternalSqlServerConnectionStringAsync()
        => GetAsync(SettingKeys.TicketLookupSqlServerConnectionString);

    public Task SetExternalSqlServerConnectionStringAsync(string? connectionString)
        => SetAsync(SettingKeys.TicketLookupSqlServerConnectionString, connectionString, isEncrypted: true);
}
