using Dapper;
using ReleasePlanGenerator.Web.Data;
using ReleasePlanGenerator.Web.Models;

namespace ReleasePlanGenerator.Web.Repositories;

public class SystemRepository(ISqliteConnectionFactory connectionFactory) : ISystemRepository
{
    public async Task<IEnumerable<SystemEntry>> GetAllAsync()
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QueryAsync<SystemEntry>("SELECT * FROM SystemEntries ORDER BY Name;");
    }

    public async Task<IEnumerable<SystemEntry>> GetActiveAsync()
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QueryAsync<SystemEntry>("SELECT * FROM SystemEntries WHERE IsActive = 1 ORDER BY Name;");
    }

    public async Task<SystemEntry?> GetByIdAsync(int id)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<SystemEntry>(
            "SELECT * FROM SystemEntries WHERE Id = @Id;", new { Id = id });
    }

    public async Task<int> CreateAsync(SystemEntry system)
    {
        using var connection = connectionFactory.CreateConnection();
        var now = DateTime.UtcNow.ToString("o");
        return await connection.ExecuteScalarAsync<int>("""
            INSERT INTO SystemEntries (Name, Description, IsActive, CreatedAt, UpdatedAt)
            VALUES (@Name, @Description, @IsActive, @CreatedAt, @UpdatedAt);
            SELECT last_insert_rowid();
            """, new { system.Name, system.Description, system.IsActive, CreatedAt = now, UpdatedAt = now });
    }

    public async Task UpdateAsync(SystemEntry system)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync("""
            UPDATE SystemEntries SET Name = @Name, Description = @Description, IsActive = @IsActive, UpdatedAt = @UpdatedAt
            WHERE Id = @Id;
            """, new { system.Name, system.Description, system.IsActive, UpdatedAt = DateTime.UtcNow.ToString("o"), system.Id });
    }

    public async Task DeactivateAsync(int id)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            "UPDATE SystemEntries SET IsActive = 0, UpdatedAt = @UpdatedAt WHERE Id = @Id;",
            new { UpdatedAt = DateTime.UtcNow.ToString("o"), Id = id });
    }
}
