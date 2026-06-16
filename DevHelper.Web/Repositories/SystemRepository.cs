using Dapper;
using DevHelper.Web.Data;
using DevHelper.Web.Models;

namespace DevHelper.Web.Repositories;

public interface ISystemRepository
{
    Task<IReadOnlyList<SystemEntry>> GetAllAsync(bool includeInactive = false);
    Task<SystemEntry?> GetByIdAsync(long id);
    Task<long> CreateAsync(SystemEntry entry);
    Task UpdateAsync(SystemEntry entry);
    Task SetActiveAsync(long id, bool isActive);
}

public sealed class SystemRepository : ISystemRepository
{
    private readonly ISqliteConnectionFactory _factory;
    private readonly ILogger<SystemRepository> _logger;

    public SystemRepository(ISqliteConnectionFactory factory, ILogger<SystemRepository> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SystemEntry>> GetAllAsync(bool includeInactive = false)
    {
        try
        {
            using var connection = _factory.CreateConnection();
            var sql = "SELECT * FROM SystemEntries"
                      + (includeInactive ? string.Empty : " WHERE IsActive = 1")
                      + " ORDER BY Name COLLATE NOCASE;";
            return (await connection.QueryAsync<SystemEntry>(sql)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load systems.");
            throw;
        }
    }

    public async Task<SystemEntry?> GetByIdAsync(long id)
    {
        using var connection = _factory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<SystemEntry>(
            "SELECT * FROM SystemEntries WHERE Id = @id;", new { id });
    }

    public async Task<long> CreateAsync(SystemEntry entry)
    {
        try
        {
            var now = DateTime.UtcNow.ToString("O");
            entry.CreatedAt = now;
            entry.UpdatedAt = now;
            using var connection = _factory.CreateConnection();
            return await connection.ExecuteScalarAsync<long>(
                """
                INSERT INTO SystemEntries (Name, Description, IsActive, CreatedAt, UpdatedAt)
                VALUES (@Name, @Description, @IsActive, @CreatedAt, @UpdatedAt);
                SELECT last_insert_rowid();
                """, entry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create system {SystemName}.", entry.Name);
            throw;
        }
    }

    public async Task UpdateAsync(SystemEntry entry)
    {
        try
        {
            entry.UpdatedAt = DateTime.UtcNow.ToString("O");
            using var connection = _factory.CreateConnection();
            await connection.ExecuteAsync(
                """
                UPDATE SystemEntries
                SET Name = @Name, Description = @Description, IsActive = @IsActive, UpdatedAt = @UpdatedAt
                WHERE Id = @Id;
                """, entry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update system {SystemId}.", entry.Id);
            throw;
        }
    }

    public async Task SetActiveAsync(long id, bool isActive)
    {
        using var connection = _factory.CreateConnection();
        await connection.ExecuteAsync(
            "UPDATE SystemEntries SET IsActive = @isActive, UpdatedAt = @now WHERE Id = @id;",
            new { id, isActive, now = DateTime.UtcNow.ToString("O") });
    }
}
