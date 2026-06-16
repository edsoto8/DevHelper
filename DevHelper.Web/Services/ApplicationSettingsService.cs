using Microsoft.AspNetCore.DataProtection;
using DevHelper.Web.Repositories;

namespace DevHelper.Web.Services;

/// <inheritdoc />
public sealed class ApplicationSettingsService : IApplicationSettingsService
{
    /// <summary>Stable Data Protection purpose string per spec section 9.15.</summary>
    public const string DataProtectionPurpose = "DevHelper.ApplicationSettings.v1";

    private readonly IApplicationSettingRepository _repository;
    private readonly IDataProtector _protector;
    private readonly ILogger<ApplicationSettingsService> _logger;

    public ApplicationSettingsService(
        IApplicationSettingRepository repository,
        IDataProtectionProvider dataProtectionProvider,
        ILogger<ApplicationSettingsService> logger)
    {
        _repository = repository;
        _protector = dataProtectionProvider.CreateProtector(DataProtectionPurpose);
        _logger = logger;
    }

    public async Task<string?> GetAsync(string key)
    {
        var setting = await _repository.GetAsync(key);
        if (setting?.SettingValue is null)
        {
            return null;
        }

        if (!setting.IsEncrypted)
        {
            return setting.SettingValue;
        }

        try
        {
            return _protector.Unprotect(setting.SettingValue);
        }
        catch (Exception ex)
        {
            // Do not log the encrypted or decrypted value.
            _logger.LogError(ex, "Failed to decrypt setting {SettingKey}.", key);
            return null;
        }
    }

    public async Task SetAsync(string key, string? value)
    {
        var isSensitive = SettingKeys.Sensitive.Contains(key);
        string? storedValue = value;

        if (isSensitive && !string.IsNullOrEmpty(value))
        {
            storedValue = _protector.Protect(value);
        }

        await _repository.UpsertAsync(key, storedValue, isSensitive && !string.IsNullOrEmpty(value));
        _logger.LogInformation("Updated application setting {SettingKey} (encrypted={Encrypted}).",
            key, isSensitive && !string.IsNullOrEmpty(value));
    }

    public Task<string?> GetScreenshotSourceDirectoryAsync() => GetAsync(SettingKeys.ScreenshotSourceDirectory);

    public Task<string?> GetDefaultEnvironmentAsync() => GetAsync(SettingKeys.DefaultEnvironment);

    public Task<string?> GetTicketLookupConnectionStringAsync() => GetAsync(SettingKeys.TicketLookupSqlServerConnectionString);
}
