using Dapper;
using ReleasePlanGenerator.Web.Data;
using ReleasePlanGenerator.Web.Models;

namespace ReleasePlanGenerator.Web.Repositories;

public class ApplicationSettingRepository(ISqliteConnectionFactory connectionFactory) : IApplicationSettingRepository
{
    public async Task<ApplicationSetting?> GetByKeyAsync(string key)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<ApplicationSetting>(
            "SELECT * FROM ApplicationSettings WHERE SettingKey = @Key;", new { Key = key });
    }

    public async Task<IEnumerable<ApplicationSetting>> GetAllAsync()
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QueryAsync<ApplicationSetting>(
            "SELECT * FROM ApplicationSettings ORDER BY SettingKey;");
    }

    public async Task UpsertAsync(string key, string? value, bool isEncrypted = false)
    {
        using var connection = connectionFactory.CreateConnection();
        var now = DateTime.UtcNow.ToString("o");
        await connection.ExecuteAsync("""
            INSERT INTO ApplicationSettings (SettingKey, SettingValue, IsEncrypted, CreatedAt, UpdatedAt)
            VALUES (@Key, @Value, @IsEncrypted, @Now, @Now)
            ON CONFLICT(SettingKey) DO UPDATE SET
                SettingValue = excluded.SettingValue,
                IsEncrypted = excluded.IsEncrypted,
                UpdatedAt = excluded.UpdatedAt;
            """, new { Key = key, Value = value, IsEncrypted = isEncrypted, Now = now });
    }
}
