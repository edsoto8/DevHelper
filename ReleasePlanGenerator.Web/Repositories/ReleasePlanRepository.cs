using Dapper;
using ReleasePlanGenerator.Web.Data;
using ReleasePlanGenerator.Web.Models;
using Serilog;

namespace ReleasePlanGenerator.Web.Repositories;

public class ReleasePlanRepository(ISqliteConnectionFactory connectionFactory) : IReleasePlanRepository
{
    public async Task<IEnumerable<ReleasePlan>> GetAllAsync()
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QueryAsync<ReleasePlan>("""
            SELECT Id, Title, ReleaseDate, Environment, CreatedBy, TemplateId,
                   MarkdownOutput, PdfFilePath, CreatedAt, UpdatedAt
            FROM ReleasePlans
            ORDER BY ReleaseDate DESC;
            """);
    }

    public async Task<ReleasePlan?> GetByIdAsync(int id)
    {
        using var connection = connectionFactory.CreateConnection();
        connection.Open();

        var plan = await connection.QuerySingleOrDefaultAsync<ReleasePlan>("""
            SELECT Id, Title, ReleaseDate, Environment, CreatedBy, TemplateId,
                   DeploymentSteps, ValidationSteps, RollbackSteps, Notes,
                   MarkdownOutput, PdfFilePath, CreatedAt, UpdatedAt
            FROM ReleasePlans WHERE Id = @Id;
            """, new { Id = id });

        if (plan is null) return null;

        plan.Tickets = (await connection.QueryAsync<ReleasePlanTicket>(
            "SELECT * FROM ReleasePlanTickets WHERE ReleasePlanId = @Id ORDER BY SortOrder;",
            new { Id = id })).ToList();

        plan.SqlScripts = (await connection.QueryAsync<SqlScript>(
            "SELECT * FROM SqlScripts WHERE ReleasePlanId = @Id ORDER BY ExecutionOrder;",
            new { Id = id })).ToList();

        plan.SelectedSystemIds = (await connection.QueryAsync<int>(
            "SELECT SystemEntryId FROM ReleasePlanSystems WHERE ReleasePlanId = @Id;",
            new { Id = id })).ToList();

        plan.SelectedServerIds = (await connection.QueryAsync<int>(
            "SELECT ServerEntryId FROM ReleasePlanServers WHERE ReleasePlanId = @Id;",
            new { Id = id })).ToList();

        plan.SelectedDatabaseIds = (await connection.QueryAsync<int>(
            "SELECT DatabaseEntryId FROM ReleasePlanDatabases WHERE ReleasePlanId = @Id;",
            new { Id = id })).ToList();

        return plan;
    }

    public async Task<int> CreateAsync(ReleasePlan plan)
    {
        using var connection = connectionFactory.CreateConnection();
        var now = DateTime.UtcNow.ToString("o");
        return await connection.ExecuteScalarAsync<int>("""
            INSERT INTO ReleasePlans
                (Title, ReleaseDate, Environment, CreatedBy, TemplateId,
                 DeploymentSteps, ValidationSteps, RollbackSteps, Notes,
                 MarkdownOutput, PdfFilePath, CreatedAt, UpdatedAt)
            VALUES
                (@Title, @ReleaseDate, @Environment, @CreatedBy, @TemplateId,
                 @DeploymentSteps, @ValidationSteps, @RollbackSteps, @Notes,
                 @MarkdownOutput, @PdfFilePath, @CreatedAt, @UpdatedAt);
            SELECT last_insert_rowid();
            """, new
        {
            plan.Title, plan.ReleaseDate, plan.Environment, plan.CreatedBy, plan.TemplateId,
            plan.DeploymentSteps, plan.ValidationSteps, plan.RollbackSteps, plan.Notes,
            plan.MarkdownOutput, plan.PdfFilePath, CreatedAt = now, UpdatedAt = now
        });
    }

    public async Task UpdateAsync(ReleasePlan plan)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync("""
            UPDATE ReleasePlans SET
                Title = @Title, ReleaseDate = @ReleaseDate, Environment = @Environment,
                CreatedBy = @CreatedBy, TemplateId = @TemplateId,
                DeploymentSteps = @DeploymentSteps, ValidationSteps = @ValidationSteps,
                RollbackSteps = @RollbackSteps, Notes = @Notes,
                MarkdownOutput = @MarkdownOutput, PdfFilePath = @PdfFilePath,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id;
            """, new
        {
            plan.Title, plan.ReleaseDate, plan.Environment, plan.CreatedBy, plan.TemplateId,
            plan.DeploymentSteps, plan.ValidationSteps, plan.RollbackSteps, plan.Notes,
            plan.MarkdownOutput, plan.PdfFilePath, UpdatedAt = DateTime.UtcNow.ToString("o"), plan.Id
        });
    }

    public async Task DeleteAsync(int id)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync("DELETE FROM ReleasePlans WHERE Id = @Id;", new { Id = id });
    }

    public async Task SaveWithChildrenAsync(ReleasePlan plan)
    {
        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var tx = connection.BeginTransaction();

        try
        {
            var now = DateTime.UtcNow.ToString("o");

            if (plan.Id == 0)
            {
                plan.Id = await connection.ExecuteScalarAsync<int>("""
                    INSERT INTO ReleasePlans
                        (Title, ReleaseDate, Environment, CreatedBy, TemplateId,
                         DeploymentSteps, ValidationSteps, RollbackSteps, Notes,
                         MarkdownOutput, PdfFilePath, CreatedAt, UpdatedAt)
                    VALUES
                        (@Title, @ReleaseDate, @Environment, @CreatedBy, @TemplateId,
                         @DeploymentSteps, @ValidationSteps, @RollbackSteps, @Notes,
                         @MarkdownOutput, @PdfFilePath, @CreatedAt, @UpdatedAt);
                    SELECT last_insert_rowid();
                    """, new
                {
                    plan.Title, plan.ReleaseDate, plan.Environment, plan.CreatedBy, plan.TemplateId,
                    plan.DeploymentSteps, plan.ValidationSteps, plan.RollbackSteps, plan.Notes,
                    plan.MarkdownOutput, plan.PdfFilePath, CreatedAt = now, UpdatedAt = now
                }, tx);
            }
            else
            {
                await connection.ExecuteAsync("""
                    UPDATE ReleasePlans SET
                        Title = @Title, ReleaseDate = @ReleaseDate, Environment = @Environment,
                        CreatedBy = @CreatedBy, TemplateId = @TemplateId,
                        DeploymentSteps = @DeploymentSteps, ValidationSteps = @ValidationSteps,
                        RollbackSteps = @RollbackSteps, Notes = @Notes,
                        MarkdownOutput = @MarkdownOutput, PdfFilePath = @PdfFilePath,
                        UpdatedAt = @UpdatedAt
                    WHERE Id = @Id;
                    """, new
                {
                    plan.Title, plan.ReleaseDate, plan.Environment, plan.CreatedBy, plan.TemplateId,
                    plan.DeploymentSteps, plan.ValidationSteps, plan.RollbackSteps, plan.Notes,
                    plan.MarkdownOutput, plan.PdfFilePath, UpdatedAt = now, plan.Id
                }, tx);

                await connection.ExecuteAsync("DELETE FROM ReleasePlanTickets WHERE ReleasePlanId = @Id;", new { plan.Id }, tx);
                await connection.ExecuteAsync("DELETE FROM SqlScripts WHERE ReleasePlanId = @Id;", new { plan.Id }, tx);
                await connection.ExecuteAsync("DELETE FROM ReleasePlanSystems WHERE ReleasePlanId = @Id;", new { plan.Id }, tx);
                await connection.ExecuteAsync("DELETE FROM ReleasePlanServers WHERE ReleasePlanId = @Id;", new { plan.Id }, tx);
                await connection.ExecuteAsync("DELETE FROM ReleasePlanDatabases WHERE ReleasePlanId = @Id;", new { plan.Id }, tx);
            }

            foreach (var ticket in plan.Tickets)
            {
                await connection.ExecuteAsync("""
                    INSERT INTO ReleasePlanTickets
                        (ReleasePlanId, TicketNumber, TicketName, TicketSummary, SortOrder, CreatedAt, UpdatedAt)
                    VALUES (@ReleasePlanId, @TicketNumber, @TicketName, @TicketSummary, @SortOrder, @CreatedAt, @UpdatedAt);
                    """, new
                {
                    ReleasePlanId = plan.Id, ticket.TicketNumber, ticket.TicketName,
                    ticket.TicketSummary, ticket.SortOrder, CreatedAt = now, UpdatedAt = now
                }, tx);
            }

            foreach (var script in plan.SqlScripts)
            {
                await connection.ExecuteAsync("""
                    INSERT INTO SqlScripts
                        (ReleasePlanId, DatabaseEntryId, ScriptName, ScriptDescription, ExecutionOrder, IsRequired, Notes)
                    VALUES (@ReleasePlanId, @DatabaseEntryId, @ScriptName, @ScriptDescription, @ExecutionOrder, @IsRequired, @Notes);
                    """, new
                {
                    ReleasePlanId = plan.Id, script.DatabaseEntryId, script.ScriptName,
                    script.ScriptDescription, script.ExecutionOrder, script.IsRequired, script.Notes
                }, tx);
            }

            foreach (var sysId in plan.SelectedSystemIds)
                await connection.ExecuteAsync(
                    "INSERT OR IGNORE INTO ReleasePlanSystems (ReleasePlanId, SystemEntryId) VALUES (@PlanId, @SysId);",
                    new { PlanId = plan.Id, SysId = sysId }, tx);

            foreach (var srvId in plan.SelectedServerIds)
                await connection.ExecuteAsync(
                    "INSERT OR IGNORE INTO ReleasePlanServers (ReleasePlanId, ServerEntryId) VALUES (@PlanId, @SrvId);",
                    new { PlanId = plan.Id, SrvId = srvId }, tx);

            foreach (var dbId in plan.SelectedDatabaseIds)
                await connection.ExecuteAsync(
                    "INSERT OR IGNORE INTO ReleasePlanDatabases (ReleasePlanId, DatabaseEntryId) VALUES (@PlanId, @DbId);",
                    new { PlanId = plan.Id, DbId = dbId }, tx);

            tx.Commit();
            Log.Information("Release plan saved. ReleasePlanId: {ReleasePlanId}", plan.Id);
        }
        catch (Exception ex)
        {
            tx.Rollback();
            Log.Error(ex, "Failed to save release plan.");
            throw;
        }
    }
}
