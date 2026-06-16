using Dapper;
using DevHelper.Web.Data;
using DevHelper.Web.Models;

namespace DevHelper.Web.Repositories;

public interface IServerRepository
{
    Task<IReadOnlyList<ServerEntry>> GetAllAsync(bool includeInactive = false);
    Task<ServerEntry?> GetByIdAsync(long id);
    Task<long> CreateAsync(ServerEntry entry);
    Task UpdateAsync(ServerEntry entry);
    Task SetActiveAsync(long id, bool isActive);
}

public sealed class ServerRepository : IServerRepository
{
    private readonly ISqliteConnectionFactory _factory;
    private readonly ILogger<ServerRepository> _logger;

    public ServerRepository(ISqliteConnectionFactory factory, ILogger<ServerRepository> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ServerEntry>> GetAllAsync(bool includeInactive = false)
    {
        try
        {
            using var connection = _factory.CreateConnection();
            var sql = "SELECT * FROM ServerEntries"
                      + (includeInactive ? string.Empty : " WHERE IsActive = 1")
                      + " ORDER BY Name COLLATE NOCASE;";
            return (await connection.QueryAsync<ServerEntry>(sql)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load servers.");
            throw;
        }
    }

    public async Task<ServerEntry?> GetByIdAsync(long id)
    {
        using var connection = _factory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<ServerEntry>(
            "SELECT * FROM ServerEntries WHERE Id = @id;", new { id });
    }

    public async Task<long> CreateAsync(ServerEntry entry)
    {
        try
        {
            var now = DateTime.UtcNow.ToString("O");
            entry.CreatedAt = now;
            entry.UpdatedAt = now;
            using var connection = _factory.CreateConnection();
            return await connection.ExecuteScalarAsync<long>(
                """
                INSERT INTO ServerEntries (Name, Environment, SystemEntryId, ServerType, Notes, IsActive, CreatedAt, UpdatedAt)
                VALUES (@Name, @Environment, @SystemEntryId, @ServerType, @Notes, @IsActive, @CreatedAt, @UpdatedAt);
                SELECT last_insert_rowid();
                """, entry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create server {ServerName}.", entry.Name);
            throw;
        }
    }

    public async Task UpdateAsync(ServerEntry entry)
    {
        try
        {
            entry.UpdatedAt = DateTime.UtcNow.ToString("O");
            using var connection = _factory.CreateConnection();
            await connection.ExecuteAsync(
                """
                UPDATE ServerEntries
                SET Name = @Name, Environment = @Environment, SystemEntryId = @SystemEntryId,
                    ServerType = @ServerType, Notes = @Notes, IsActive = @IsActive, UpdatedAt = @UpdatedAt
                WHERE Id = @Id;
                """, entry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update server {ServerId}.", entry.Id);
            throw;
        }
    }

    public async Task SetActiveAsync(long id, bool isActive)
    {
        using var connection = _factory.CreateConnection();
        await connection.ExecuteAsync(
            "UPDATE ServerEntries SET IsActive = @isActive, UpdatedAt = @now WHERE Id = @id;",
            new { id, isActive, now = DateTime.UtcNow.ToString("O") });
    }
}
