using Dapper;
using DevHelper.Web.Data;
using DevHelper.Web.Models;

namespace DevHelper.Web.Repositories;

public interface IDatabaseRepository
{
    Task<IReadOnlyList<DatabaseEntry>> GetAllAsync(bool includeInactive = false);
    Task<DatabaseEntry?> GetByIdAsync(long id);
    Task<long> CreateAsync(DatabaseEntry entry);
    Task UpdateAsync(DatabaseEntry entry);
    Task SetActiveAsync(long id, bool isActive);
}

public sealed class DatabaseRepository : IDatabaseRepository
{
    private readonly ISqliteConnectionFactory _factory;
    private readonly ILogger<DatabaseRepository> _logger;

    public DatabaseRepository(ISqliteConnectionFactory factory, ILogger<DatabaseRepository> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DatabaseEntry>> GetAllAsync(bool includeInactive = false)
    {
        try
        {
            using var connection = _factory.CreateConnection();
            var sql = "SELECT * FROM DatabaseEntries"
                      + (includeInactive ? string.Empty : " WHERE IsActive = 1")
                      + " ORDER BY Name COLLATE NOCASE;";
            return (await connection.QueryAsync<DatabaseEntry>(sql)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load databases.");
            throw;
        }
    }

    public async Task<DatabaseEntry?> GetByIdAsync(long id)
    {
        using var connection = _factory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<DatabaseEntry>(
            "SELECT * FROM DatabaseEntries WHERE Id = @id;", new { id });
    }

    public async Task<long> CreateAsync(DatabaseEntry entry)
    {
        try
        {
            var now = DateTime.UtcNow.ToString("O");
            entry.CreatedAt = now;
            entry.UpdatedAt = now;
            using var connection = _factory.CreateConnection();
            return await connection.ExecuteScalarAsync<long>(
                """
                INSERT INTO DatabaseEntries (Name, SqlServerInstance, Environment, SystemEntryId, Notes, IsActive, CreatedAt, UpdatedAt)
                VALUES (@Name, @SqlServerInstance, @Environment, @SystemEntryId, @Notes, @IsActive, @CreatedAt, @UpdatedAt);
                SELECT last_insert_rowid();
                """, entry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create database {DatabaseName}.", entry.Name);
            throw;
        }
    }

    public async Task UpdateAsync(DatabaseEntry entry)
    {
        try
        {
            entry.UpdatedAt = DateTime.UtcNow.ToString("O");
            using var connection = _factory.CreateConnection();
            await connection.ExecuteAsync(
                """
                UPDATE DatabaseEntries
                SET Name = @Name, SqlServerInstance = @SqlServerInstance, Environment = @Environment,
                    SystemEntryId = @SystemEntryId, Notes = @Notes, IsActive = @IsActive, UpdatedAt = @UpdatedAt
                WHERE Id = @Id;
                """, entry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update database {DatabaseId}.", entry.Id);
            throw;
        }
    }

    public async Task SetActiveAsync(long id, bool isActive)
    {
        using var connection = _factory.CreateConnection();
        await connection.ExecuteAsync(
            "UPDATE DatabaseEntries SET IsActive = @isActive, UpdatedAt = @now WHERE Id = @id;",
            new { id, isActive, now = DateTime.UtcNow.ToString("O") });
    }
}
