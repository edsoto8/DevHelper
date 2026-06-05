using Dapper;
using ReleasePlanGenerator.Web.Data;
using ReleasePlanGenerator.Web.Models;

namespace ReleasePlanGenerator.Web.Repositories;

public class DatabaseRepository(ISqliteConnectionFactory connectionFactory) : IDatabaseRepository
{
    public async Task<IEnumerable<DatabaseEntry>> GetAllAsync()
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QueryAsync<DatabaseEntry>("SELECT * FROM DatabaseEntries ORDER BY Name;");
    }

    public async Task<IEnumerable<DatabaseEntry>> GetActiveAsync()
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QueryAsync<DatabaseEntry>("SELECT * FROM DatabaseEntries WHERE IsActive = 1 ORDER BY Name;");
    }

    public async Task<DatabaseEntry?> GetByIdAsync(int id)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<DatabaseEntry>(
            "SELECT * FROM DatabaseEntries WHERE Id = @Id;", new { Id = id });
    }

    public async Task<int> CreateAsync(DatabaseEntry database)
    {
        using var connection = connectionFactory.CreateConnection();
        var now = DateTime.UtcNow.ToString("o");
        return await connection.ExecuteScalarAsync<int>("""
            INSERT INTO DatabaseEntries (Name, SqlServerInstance, Environment, SystemEntryId, Notes, IsActive, CreatedAt, UpdatedAt)
            VALUES (@Name, @SqlServerInstance, @Environment, @SystemEntryId, @Notes, @IsActive, @CreatedAt, @UpdatedAt);
            SELECT last_insert_rowid();
            """, new { database.Name, database.SqlServerInstance, database.Environment, database.SystemEntryId, database.Notes, database.IsActive, CreatedAt = now, UpdatedAt = now });
    }

    public async Task UpdateAsync(DatabaseEntry database)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync("""
            UPDATE DatabaseEntries SET
                Name = @Name, SqlServerInstance = @SqlServerInstance, Environment = @Environment,
                SystemEntryId = @SystemEntryId, Notes = @Notes, IsActive = @IsActive, UpdatedAt = @UpdatedAt
            WHERE Id = @Id;
            """, new { database.Name, database.SqlServerInstance, database.Environment, database.SystemEntryId, database.Notes, database.IsActive, UpdatedAt = DateTime.UtcNow.ToString("o"), database.Id });
    }

    public async Task DeactivateAsync(int id)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            "UPDATE DatabaseEntries SET IsActive = 0, UpdatedAt = @UpdatedAt WHERE Id = @Id;",
            new { UpdatedAt = DateTime.UtcNow.ToString("o"), Id = id });
    }
}
