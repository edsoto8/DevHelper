using Dapper;
using ReleasePlanGenerator.Web.Data;
using ReleasePlanGenerator.Web.Models;

namespace ReleasePlanGenerator.Web.Repositories;

public class ReleaseTemplateRepository(ISqliteConnectionFactory connectionFactory) : IReleaseTemplateRepository
{
    public async Task<IEnumerable<ReleaseTemplate>> GetAllAsync()
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QueryAsync<ReleaseTemplate>(
            "SELECT * FROM ReleaseTemplates ORDER BY Name;");
    }

    public async Task<IEnumerable<ReleaseTemplate>> GetActiveAsync()
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QueryAsync<ReleaseTemplate>(
            "SELECT * FROM ReleaseTemplates WHERE IsActive = 1 ORDER BY IsDefault DESC, Name;");
    }

    public async Task<ReleaseTemplate?> GetByIdAsync(int id)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<ReleaseTemplate>(
            "SELECT * FROM ReleaseTemplates WHERE Id = @Id;", new { Id = id });
    }

    public async Task<ReleaseTemplate?> GetDefaultAsync()
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<ReleaseTemplate>(
            "SELECT * FROM ReleaseTemplates WHERE IsDefault = 1 AND IsActive = 1 LIMIT 1;");
    }

    public async Task<int> CreateAsync(ReleaseTemplate template)
    {
        using var connection = connectionFactory.CreateConnection();
        var now = DateTime.UtcNow.ToString("o");
        return await connection.ExecuteScalarAsync<int>("""
            INSERT INTO ReleaseTemplates (Name, Description, MarkdownTemplate, IsDefault, IsActive, CreatedAt, UpdatedAt)
            VALUES (@Name, @Description, @MarkdownTemplate, @IsDefault, @IsActive, @CreatedAt, @UpdatedAt);
            SELECT last_insert_rowid();
            """, new { template.Name, template.Description, template.MarkdownTemplate, template.IsDefault, template.IsActive, CreatedAt = now, UpdatedAt = now });
    }

    public async Task UpdateAsync(ReleaseTemplate template)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync("""
            UPDATE ReleaseTemplates SET
                Name = @Name, Description = @Description, MarkdownTemplate = @MarkdownTemplate,
                IsDefault = @IsDefault, IsActive = @IsActive, UpdatedAt = @UpdatedAt
            WHERE Id = @Id;
            """, new { template.Name, template.Description, template.MarkdownTemplate, template.IsDefault, template.IsActive, UpdatedAt = DateTime.UtcNow.ToString("o"), template.Id });
    }

    public async Task SetDefaultAsync(int id)
    {
        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var tx = connection.BeginTransaction();
        await connection.ExecuteAsync("UPDATE ReleaseTemplates SET IsDefault = 0;", transaction: tx);
        await connection.ExecuteAsync("UPDATE ReleaseTemplates SET IsDefault = 1 WHERE Id = @Id;", new { Id = id }, tx);
        tx.Commit();
    }

    public async Task DeleteAsync(int id)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync("UPDATE ReleaseTemplates SET IsActive = 0 WHERE Id = @Id;", new { Id = id });
    }
}
