using Dapper;
using DevHelper.Web.Data;
using DevHelper.Web.Models;

namespace DevHelper.Web.Repositories;

public interface ITemplateRepository
{
    Task<IReadOnlyList<Template>> GetByToolTypeAsync(string toolType, bool includeInactive = false);
    Task<Template?> GetByIdAsync(long id);
    Task<Template?> GetDefaultAsync(string toolType);
    Task<long> CreateAsync(Template template);
    Task UpdateAsync(Template template);
    Task SetActiveAsync(long id, bool isActive);
    Task SetDefaultAsync(long id, string toolType);
    Task<long> DuplicateAsync(long id);
}

public sealed class TemplateRepository : ITemplateRepository
{
    private readonly ISqliteConnectionFactory _factory;
    private readonly ILogger<TemplateRepository> _logger;

    public TemplateRepository(ISqliteConnectionFactory factory, ILogger<TemplateRepository> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Template>> GetByToolTypeAsync(string toolType, bool includeInactive = false)
    {
        using var connection = _factory.CreateConnection();
        var sql = "SELECT * FROM Templates WHERE ToolType = @toolType"
                  + (includeInactive ? string.Empty : " AND IsActive = 1")
                  + " ORDER BY IsDefault DESC, Name COLLATE NOCASE;";
        return (await connection.QueryAsync<Template>(sql, new { toolType })).ToList();
    }

    public async Task<Template?> GetByIdAsync(long id)
    {
        using var connection = _factory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Template>(
            "SELECT * FROM Templates WHERE Id = @id;", new { id });
    }

    public async Task<Template?> GetDefaultAsync(string toolType)
    {
        using var connection = _factory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Template>(
            "SELECT * FROM Templates WHERE ToolType = @toolType AND IsDefault = 1 AND IsActive = 1 LIMIT 1;",
            new { toolType });
    }

    public async Task<long> CreateAsync(Template template)
    {
        try
        {
            var now = DateTime.UtcNow.ToString("O");
            template.CreatedAt = now;
            template.UpdatedAt = now;
            using var connection = _factory.CreateConnection();
            return await connection.ExecuteScalarAsync<long>(
                """
                INSERT INTO Templates (ToolType, Name, Description, MarkdownTemplate, IsDefault, IsActive, CreatedAt, UpdatedAt)
                VALUES (@ToolType, @Name, @Description, @MarkdownTemplate, @IsDefault, @IsActive, @CreatedAt, @UpdatedAt);
                SELECT last_insert_rowid();
                """, template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create template {TemplateName}.", template.Name);
            throw;
        }
    }

    public async Task UpdateAsync(Template template)
    {
        try
        {
            template.UpdatedAt = DateTime.UtcNow.ToString("O");
            using var connection = _factory.CreateConnection();
            await connection.ExecuteAsync(
                """
                UPDATE Templates
                SET Name = @Name, Description = @Description, MarkdownTemplate = @MarkdownTemplate,
                    IsActive = @IsActive, UpdatedAt = @UpdatedAt
                WHERE Id = @Id;
                """, template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update template {TemplateId}.", template.Id);
            throw;
        }
    }

    public async Task SetActiveAsync(long id, bool isActive)
    {
        using var connection = _factory.CreateConnection();
        await connection.ExecuteAsync(
            "UPDATE Templates SET IsActive = @isActive, UpdatedAt = @now WHERE Id = @id;",
            new { id, isActive, now = DateTime.UtcNow.ToString("O") });
    }

    public async Task SetDefaultAsync(long id, string toolType)
    {
        try
        {
            using var connection = _factory.CreateConnection();
            using var transaction = connection.BeginTransaction();
            var now = DateTime.UtcNow.ToString("O");
            await connection.ExecuteAsync(
                "UPDATE Templates SET IsDefault = 0, UpdatedAt = @now WHERE ToolType = @toolType AND IsDefault = 1;",
                new { toolType, now }, transaction);
            await connection.ExecuteAsync(
                "UPDATE Templates SET IsDefault = 1, UpdatedAt = @now WHERE Id = @id;",
                new { id, now }, transaction);
            transaction.Commit();
            _logger.LogInformation("Set template {TemplateId} as default for {ToolType}.", id, toolType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set default template {TemplateId}.", id);
            throw;
        }
    }

    public async Task<long> DuplicateAsync(long id)
    {
        var source = await GetByIdAsync(id)
            ?? throw new InvalidOperationException($"Template {id} not found.");
        var copy = new Template
        {
            ToolType = source.ToolType,
            Name = $"{source.Name} (Copy)",
            Description = source.Description,
            MarkdownTemplate = source.MarkdownTemplate,
            IsDefault = false,
            IsActive = true,
        };
        return await CreateAsync(copy);
    }
}
