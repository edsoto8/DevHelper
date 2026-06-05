using Dapper;
using ReleasePlanGenerator.Web.Data;
using ReleasePlanGenerator.Web.Models;

namespace ReleasePlanGenerator.Web.Repositories;

public class ServerRepository(ISqliteConnectionFactory connectionFactory) : IServerRepository
{
    public async Task<IEnumerable<ServerEntry>> GetAllAsync()
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QueryAsync<ServerEntry>("SELECT * FROM ServerEntries ORDER BY Name;");
    }

    public async Task<IEnumerable<ServerEntry>> GetActiveAsync()
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QueryAsync<ServerEntry>("SELECT * FROM ServerEntries WHERE IsActive = 1 ORDER BY Name;");
    }

    public async Task<ServerEntry?> GetByIdAsync(int id)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<ServerEntry>(
            "SELECT * FROM ServerEntries WHERE Id = @Id;", new { Id = id });
    }

    public async Task<int> CreateAsync(ServerEntry server)
    {
        using var connection = connectionFactory.CreateConnection();
        var now = DateTime.UtcNow.ToString("o");
        return await connection.ExecuteScalarAsync<int>("""
            INSERT INTO ServerEntries (Name, Environment, SystemEntryId, ServerType, Notes, IsActive, CreatedAt, UpdatedAt)
            VALUES (@Name, @Environment, @SystemEntryId, @ServerType, @Notes, @IsActive, @CreatedAt, @UpdatedAt);
            SELECT last_insert_rowid();
            """, new { server.Name, server.Environment, server.SystemEntryId, server.ServerType, server.Notes, server.IsActive, CreatedAt = now, UpdatedAt = now });
    }

    public async Task UpdateAsync(ServerEntry server)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync("""
            UPDATE ServerEntries SET
                Name = @Name, Environment = @Environment, SystemEntryId = @SystemEntryId,
                ServerType = @ServerType, Notes = @Notes, IsActive = @IsActive, UpdatedAt = @UpdatedAt
            WHERE Id = @Id;
            """, new { server.Name, server.Environment, server.SystemEntryId, server.ServerType, server.Notes, server.IsActive, UpdatedAt = DateTime.UtcNow.ToString("o"), server.Id });
    }

    public async Task DeactivateAsync(int id)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            "UPDATE ServerEntries SET IsActive = 0, UpdatedAt = @UpdatedAt WHERE Id = @Id;",
            new { UpdatedAt = DateTime.UtcNow.ToString("o"), Id = id });
    }
}
