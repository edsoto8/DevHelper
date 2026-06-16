using Dapper;
using DevHelper.Web.Data;
using DevHelper.Web.Models;

namespace DevHelper.Web.Repositories;

public interface IApplicationSettingRepository
{
    Task<ApplicationSetting?> GetAsync(string key);
    Task<IReadOnlyList<ApplicationSetting>> GetAllAsync();
    Task UpsertAsync(string key, string? value, bool isEncrypted);
}

public sealed class ApplicationSettingRepository : IApplicationSettingRepository
{
    private readonly ISqliteConnectionFactory _factory;
    private readonly ILogger<ApplicationSettingRepository> _logger;

    public ApplicationSettingRepository(ISqliteConnectionFactory factory, ILogger<ApplicationSettingRepository> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<ApplicationSetting?> GetAsync(string key)
    {
        using var connection = _factory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<ApplicationSetting>(
            "SELECT * FROM ApplicationSettings WHERE SettingKey = @key;", new { key });
    }

    public async Task<IReadOnlyList<ApplicationSetting>> GetAllAsync()
    {
        using var connection = _factory.CreateConnection();
        return (await connection.QueryAsync<ApplicationSetting>(
            "SELECT * FROM ApplicationSettings ORDER BY SettingKey;")).ToList();
    }

    public async Task UpsertAsync(string key, string? value, bool isEncrypted)
    {
        try
        {
            var now = DateTime.UtcNow.ToString("O");
            using var connection = _factory.CreateConnection();
            await connection.ExecuteAsync(
                """
                INSERT INTO ApplicationSettings (SettingKey, SettingValue, IsEncrypted, CreatedAt, UpdatedAt)
                VALUES (@key, @value, @isEncrypted, @now, @now)
                ON CONFLICT(SettingKey) DO UPDATE SET
                    SettingValue = excluded.SettingValue,
                    IsEncrypted = excluded.IsEncrypted,
                    UpdatedAt = excluded.UpdatedAt;
                """, new { key, value, isEncrypted, now });
        }
        catch (Exception ex)
        {
            // Never log the value; it may be sensitive.
            _logger.LogError(ex, "Failed to upsert application setting {SettingKey}.", key);
            throw;
        }
    }
}
