using System.Data;
using Dapper;
using DevHelper.Web.Data;
using DevHelper.Web.Models;

namespace DevHelper.Web.Repositories;

/// <summary>A lightweight row for dashboard listing.</summary>
public sealed class ReleasePlanSummary
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ReleaseDate { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
}

public interface IReleasePlanRepository
{
    Task<IReadOnlyList<ReleasePlanSummary>> SearchAsync(string? titleQuery, string? environment);
    Task<ReleasePlanAggregate?> GetAggregateAsync(long id);
    Task<long> SaveAsync(ReleasePlanAggregate aggregate);
    Task DeleteAsync(long id);
}

public sealed class ReleasePlanRepository : IReleasePlanRepository
{
    private readonly ISqliteConnectionFactory _factory;
    private readonly ILogger<ReleasePlanRepository> _logger;

    public ReleasePlanRepository(ISqliteConnectionFactory factory, ILogger<ReleasePlanRepository> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ReleasePlanSummary>> SearchAsync(string? titleQuery, string? environment)
    {
        using var connection = _factory.CreateConnection();
        var sql = """
            SELECT Id, Title, ReleaseDate, Environment, CreatedBy, UpdatedAt
            FROM ReleasePlans
            WHERE (@titleQuery IS NULL OR Title LIKE '%' || @titleQuery || '%')
              AND (@environment IS NULL OR Environment = @environment)
            ORDER BY datetime(UpdatedAt) DESC;
            """;
        return (await connection.QueryAsync<ReleasePlanSummary>(
            sql, new { titleQuery, environment })).ToList();
    }

    public async Task<ReleasePlanAggregate?> GetAggregateAsync(long id)
    {
        using var connection = _factory.CreateConnection();
        var plan = await connection.QuerySingleOrDefaultAsync<ReleasePlan>(
            "SELECT * FROM ReleasePlans WHERE Id = @id;", new { id });
        if (plan is null)
        {
            return null;
        }

        var aggregate = new ReleasePlanAggregate { Plan = plan };
        aggregate.Tickets = (await connection.QueryAsync<ReleasePlanTicket>(
            "SELECT * FROM ReleasePlanTickets WHERE ReleasePlanId = @id ORDER BY SortOrder, Id;", new { id })).ToList();
        aggregate.SqlScripts = (await connection.QueryAsync<SqlScript>(
            "SELECT * FROM SqlScripts WHERE ReleasePlanId = @id ORDER BY ExecutionOrder, Id;", new { id })).ToList();
        aggregate.Systems = (await connection.QueryAsync<ReleasePlanSystemLink>(
            "SELECT * FROM ReleasePlanSystems WHERE ReleasePlanId = @id ORDER BY SortOrder, Id;", new { id })).ToList();
        aggregate.Servers = (await connection.QueryAsync<ReleasePlanServerLink>(
            "SELECT * FROM ReleasePlanServers WHERE ReleasePlanId = @id ORDER BY SortOrder, Id;", new { id })).ToList();
        aggregate.Databases = (await connection.QueryAsync<ReleasePlanDatabaseLink>(
            "SELECT * FROM ReleasePlanDatabases WHERE ReleasePlanId = @id ORDER BY SortOrder, Id;", new { id })).ToList();
        aggregate.Screenshots = (await connection.QueryAsync<PlanScreenshot>(
            """
            SELECT * FROM PlanScreenshots
            WHERE PlanType = @planType AND PlanId = @id
            ORDER BY SortOrder, Id;
            """, new { planType = PlanTypes.ReleasePlan, id })).ToList();
        return aggregate;
    }

    public async Task<long> SaveAsync(ReleasePlanAggregate aggregate)
    {
        var now = DateTime.UtcNow.ToString("O");
        using var connection = _factory.CreateConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            var plan = aggregate.Plan;
            plan.UpdatedAt = now;

            if (plan.Id == 0)
            {
                plan.CreatedAt = now;
                plan.Id = await connection.ExecuteScalarAsync<long>(
                    """
                    INSERT INTO ReleasePlans
                        (Title, ReleaseDate, Environment, CreatedBy, TemplateId, BackupSteps, DeploymentSteps,
                         ValidationSteps, RollbackSteps, Notes, MarkdownOutput, PdfFilePath, CreatedAt, UpdatedAt)
                    VALUES
                        (@Title, @ReleaseDate, @Environment, @CreatedBy, @TemplateId, @BackupSteps, @DeploymentSteps,
                         @ValidationSteps, @RollbackSteps, @Notes, @MarkdownOutput, @PdfFilePath, @CreatedAt, @UpdatedAt);
                    SELECT last_insert_rowid();
                    """, plan, transaction);
            }
            else
            {
                await connection.ExecuteAsync(
                    """
                    UPDATE ReleasePlans
                    SET Title = @Title, ReleaseDate = @ReleaseDate, Environment = @Environment, CreatedBy = @CreatedBy,
                        TemplateId = @TemplateId, BackupSteps = @BackupSteps, DeploymentSteps = @DeploymentSteps,
                        ValidationSteps = @ValidationSteps, RollbackSteps = @RollbackSteps, Notes = @Notes,
                        MarkdownOutput = @MarkdownOutput, PdfFilePath = @PdfFilePath, UpdatedAt = @UpdatedAt
                    WHERE Id = @Id;
                    """, plan, transaction);

                // Replace child rows on update.
                await DeleteChildrenAsync(connection, transaction, plan.Id);
            }

            await InsertChildrenAsync(connection, transaction, plan.Id, aggregate, now);

            transaction.Commit();
            _logger.LogInformation("Saved release plan {ReleasePlanId} ({Title}).", plan.Id, plan.Title);
            return plan.Id;
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            _logger.LogError(ex, "Failed to save release plan {Title}; transaction rolled back.", aggregate.Plan.Title);
            throw;
        }
    }

    public async Task DeleteAsync(long id)
    {
        try
        {
            using var connection = _factory.CreateConnection();
            using var transaction = connection.BeginTransaction();
            // Screenshots are not FK-linked (polymorphic), so remove them explicitly.
            await connection.ExecuteAsync(
                "DELETE FROM PlanScreenshots WHERE PlanType = @planType AND PlanId = @id;",
                new { planType = PlanTypes.ReleasePlan, id }, transaction);
            // Remaining children cascade via FK ON DELETE CASCADE.
            await connection.ExecuteAsync("DELETE FROM ReleasePlans WHERE Id = @id;", new { id }, transaction);
            transaction.Commit();
            _logger.LogInformation("Deleted release plan {ReleasePlanId}.", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete release plan {ReleasePlanId}.", id);
            throw;
        }
    }

    private static async Task DeleteChildrenAsync(IDbConnection connection, IDbTransaction transaction, long planId)
    {
        await connection.ExecuteAsync("DELETE FROM ReleasePlanTickets WHERE ReleasePlanId = @planId;", new { planId }, transaction);
        await connection.ExecuteAsync("DELETE FROM SqlScripts WHERE ReleasePlanId = @planId;", new { planId }, transaction);
        await connection.ExecuteAsync("DELETE FROM ReleasePlanSystems WHERE ReleasePlanId = @planId;", new { planId }, transaction);
        await connection.ExecuteAsync("DELETE FROM ReleasePlanServers WHERE ReleasePlanId = @planId;", new { planId }, transaction);
        await connection.ExecuteAsync("DELETE FROM ReleasePlanDatabases WHERE ReleasePlanId = @planId;", new { planId }, transaction);
        await connection.ExecuteAsync(
            "DELETE FROM PlanScreenshots WHERE PlanType = @planType AND PlanId = @planId;",
            new { planType = PlanTypes.ReleasePlan, planId }, transaction);
    }

    private static async Task InsertChildrenAsync(
        IDbConnection connection, IDbTransaction transaction, long planId, ReleasePlanAggregate aggregate, string now)
    {
        foreach (var ticket in aggregate.Tickets)
        {
            ticket.ReleasePlanId = planId;
            ticket.CreatedAt = string.IsNullOrEmpty(ticket.CreatedAt) ? now : ticket.CreatedAt;
            ticket.UpdatedAt = now;
            await connection.ExecuteAsync(
                """
                INSERT INTO ReleasePlanTickets (ReleasePlanId, TicketNumber, TicketName, TicketSummary, SortOrder, CreatedAt, UpdatedAt)
                VALUES (@ReleasePlanId, @TicketNumber, @TicketName, @TicketSummary, @SortOrder, @CreatedAt, @UpdatedAt);
                """, ticket, transaction);
        }

        foreach (var script in aggregate.SqlScripts)
        {
            script.ReleasePlanId = planId;
            await connection.ExecuteAsync(
                """
                INSERT INTO SqlScripts (ReleasePlanId, DatabaseEntryId, ScriptName, ScriptDescription, ExecutionOrder, IsRequired, Notes)
                VALUES (@ReleasePlanId, @DatabaseEntryId, @ScriptName, @ScriptDescription, @ExecutionOrder, @IsRequired, @Notes);
                """, script, transaction);
        }

        foreach (var link in aggregate.Systems)
        {
            link.ReleasePlanId = planId;
            await connection.ExecuteAsync(
                """
                INSERT INTO ReleasePlanSystems (ReleasePlanId, SystemEntryId, OtherSystemName, SortOrder)
                VALUES (@ReleasePlanId, @SystemEntryId, @OtherSystemName, @SortOrder);
                """, link, transaction);
        }

        foreach (var link in aggregate.Servers)
        {
            link.ReleasePlanId = planId;
            await connection.ExecuteAsync(
                """
                INSERT INTO ReleasePlanServers (ReleasePlanId, ServerEntryId, SortOrder)
                VALUES (@ReleasePlanId, @ServerEntryId, @SortOrder);
                """, link, transaction);
        }

        foreach (var link in aggregate.Databases)
        {
            link.ReleasePlanId = planId;
            await connection.ExecuteAsync(
                """
                INSERT INTO ReleasePlanDatabases (ReleasePlanId, DatabaseEntryId, SortOrder)
                VALUES (@ReleasePlanId, @DatabaseEntryId, @SortOrder);
                """, link, transaction);
        }

        foreach (var shot in aggregate.Screenshots)
        {
            shot.PlanType = PlanTypes.ReleasePlan;
            shot.PlanId = planId;
            shot.AttachedAt = string.IsNullOrEmpty(shot.AttachedAt) ? now : shot.AttachedAt;
            shot.CreatedAt = string.IsNullOrEmpty(shot.CreatedAt) ? now : shot.CreatedAt;
            shot.UpdatedAt = now;
            await connection.ExecuteAsync(
                """
                INSERT INTO PlanScreenshots (PlanType, PlanId, Description, FilePath, AttachedAt, SortOrder, CreatedAt, UpdatedAt)
                VALUES (@PlanType, @PlanId, @Description, @FilePath, @AttachedAt, @SortOrder, @CreatedAt, @UpdatedAt);
                """, shot, transaction);
        }
    }
}
