using ReleasePlanGenerator.Web.Models;

namespace ReleasePlanGenerator.Web.Repositories;

public interface IApplicationSettingRepository
{
    Task<ApplicationSetting?> GetByKeyAsync(string key);
    Task<IEnumerable<ApplicationSetting>> GetAllAsync();
    Task UpsertAsync(string key, string? value, bool isEncrypted = false);
}
