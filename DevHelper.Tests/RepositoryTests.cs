using Dapper;
using DevHelper.Web.Models;
using DevHelper.Web.Repositories;

namespace DevHelper.Tests;

public sealed class RepositoryTests
{
    private static SystemRepository Systems(TestDatabase db)
        => new(db.ConnectionFactory, TestFactories.Logger<SystemRepository>());

    private static ReleasePlanRepository Plans(TestDatabase db)
        => new(db.ConnectionFactory, TestFactories.Logger<ReleasePlanRepository>());

    [Fact]
    public async Task SystemRepository_supports_crud()
    {
        await using var db = await TestDatabase.CreateAsync();
        var repo = Systems(db);

        var id = await repo.CreateAsync(new SystemEntry { Name = "Payments", Description = "Billing" });
        var loaded = await repo.GetByIdAsync(id);
        Assert.NotNull(loaded);
        Assert.Equal("Payments", loaded!.Name);

        loaded.Name = "Payments v2";
        await repo.UpdateAsync(loaded);
        Assert.Equal("Payments v2", (await repo.GetByIdAsync(id))!.Name);

        await repo.SetActiveAsync(id, false);
        var active = await repo.GetAllAsync(includeInactive: false);
        Assert.DoesNotContain(active, s => s.Id == id);
        var all = await repo.GetAllAsync(includeInactive: true);
        Assert.Contains(all, s => s.Id == id);
    }

    [Fact]
    public async Task ReleasePlan_save_persists_children_and_round_trips()
    {
        await using var db = await TestDatabase.CreateAsync();
        var systemId = await Systems(db).CreateAsync(new SystemEntry { Name = "Web" });
        var repo = Plans(db);

        var aggregate = BuildAggregate(systemId);
        aggregate.Tickets.Add(new ReleasePlanTicket { TicketNumber = "T-1", TicketName = "First", SortOrder = 0 });
        aggregate.Tickets.Add(new ReleasePlanTicket { TicketNumber = "T-2", TicketName = "Second", SortOrder = 1 });
        aggregate.SqlScripts.Add(new SqlScript { ScriptName = "deploy.sql", ExecutionOrder = 1, IsRequired = true });
        aggregate.Screenshots.Add(new PlanScreenshot { Description = "shot", FilePath = "a/b.png", SortOrder = 0 });

        var id = await repo.SaveAsync(aggregate);
        var loaded = await repo.GetAggregateAsync(id);

        Assert.NotNull(loaded);
        Assert.Equal(2, loaded!.Tickets.Count);
        Assert.Single(loaded.SqlScripts);
        Assert.Single(loaded.Systems);
        Assert.Single(loaded.Screenshots);
        Assert.Equal("T-1", loaded.Tickets[0].TicketNumber);
    }

    [Fact]
    public async Task ReleasePlan_update_replaces_children()
    {
        await using var db = await TestDatabase.CreateAsync();
        var systemId = await Systems(db).CreateAsync(new SystemEntry { Name = "Web" });
        var repo = Plans(db);

        var aggregate = BuildAggregate(systemId);
        aggregate.Tickets.Add(new ReleasePlanTicket { TicketNumber = "T-1", TicketName = "First" });
        var id = await repo.SaveAsync(aggregate);

        var reloaded = await repo.GetAggregateAsync(id);
        reloaded!.Tickets.Clear();
        reloaded.Tickets.Add(new ReleasePlanTicket { TicketNumber = "T-9", TicketName = "Replaced" });
        await repo.SaveAsync(reloaded);

        var final = await repo.GetAggregateAsync(id);
        Assert.Single(final!.Tickets);
        Assert.Equal("T-9", final.Tickets[0].TicketNumber);
    }

    [Fact]
    public async Task Delete_release_plan_cascades_to_children()
    {
        await using var db = await TestDatabase.CreateAsync();
        var systemId = await Systems(db).CreateAsync(new SystemEntry { Name = "Web" });
        var repo = Plans(db);

        var aggregate = BuildAggregate(systemId);
        aggregate.Tickets.Add(new ReleasePlanTicket { TicketNumber = "T-1", TicketName = "First" });
        aggregate.Screenshots.Add(new PlanScreenshot { FilePath = "x.png" });
        var id = await repo.SaveAsync(aggregate);

        await repo.DeleteAsync(id);

        using var connection = db.ConnectionFactory.CreateConnection();
        Assert.Equal(0, await connection.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM ReleasePlanTickets WHERE ReleasePlanId = @id;", new { id }));
        Assert.Equal(0, await connection.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM ReleasePlanSystems WHERE ReleasePlanId = @id;", new { id }));
        Assert.Equal(0, await connection.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM PlanScreenshots WHERE PlanType = 'ReleasePlan' AND PlanId = @id;", new { id }));
    }

    [Fact]
    public async Task Save_rolls_back_entire_transaction_on_child_failure()
    {
        await using var db = await TestDatabase.CreateAsync();
        var systemId = await Systems(db).CreateAsync(new SystemEntry { Name = "Web" });
        var repo = Plans(db);

        var aggregate = BuildAggregate(systemId);
        aggregate.Tickets.Add(new ReleasePlanTicket { TicketNumber = "T-1", TicketName = "First" });
        // Reference a server that does not exist -> FK violation mid-transaction.
        aggregate.Servers.Add(new ReleasePlanServerLink { ServerEntryId = 99999 });

        await Assert.ThrowsAnyAsync<Exception>(() => repo.SaveAsync(aggregate));

        // No partial plan row should remain.
        var all = await repo.SearchAsync(null, null);
        Assert.Empty(all);
    }

    private static ReleasePlanAggregate BuildAggregate(long systemId) => new()
    {
        Plan = new ReleasePlan
        {
            Title = "Plan",
            ReleaseDate = "2026-06-14",
            Environment = Environments.Production,
            CreatedBy = "Eduardo",
            TemplateId = 1,
        },
        Systems = { new ReleasePlanSystemLink { SystemEntryId = systemId, SortOrder = 0 } },
    };
}
