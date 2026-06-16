using DevHelper.Web.Models;
using DevHelper.Web.Repositories;
using DevHelper.Web.Services;

namespace DevHelper.Tests;

/// <summary>
/// Exercises the full release plan service graph (validation -> Markdown -> transactional
/// save) against a real temporary SQLite database with the seeded default template.
/// </summary>
public sealed class ReleasePlanServiceIntegrationTests
{
    private static ReleasePlanService BuildService(TestDatabase db, out ISystemRepository systems)
    {
        var factory = db.ConnectionFactory;
        systems = new SystemRepository(factory, TestFactories.Logger<SystemRepository>());
        var servers = new ServerRepository(factory, TestFactories.Logger<ServerRepository>());
        var databases = new DatabaseRepository(factory, TestFactories.Logger<DatabaseRepository>());
        var templates = new TemplateRepository(factory, TestFactories.Logger<TemplateRepository>());
        var plans = new ReleasePlanRepository(factory, TestFactories.Logger<ReleasePlanRepository>());
        var settingsRepo = new ApplicationSettingRepository(factory, TestFactories.Logger<ApplicationSettingRepository>());
        var settings = new ApplicationSettingsService(settingsRepo, TestFactories.DataProtection(),
            TestFactories.Logger<ApplicationSettingsService>());
        var markdown = new MarkdownGenerationService(TestFactories.Logger<MarkdownGenerationService>());
        var screenshots = new ScreenshotService(settings, TestFactories.Logger<ScreenshotService>());

        return new ReleasePlanService(plans, systems, servers, databases, templates, markdown, screenshots,
            TestFactories.Logger<ReleasePlanService>());
    }

    [Fact]
    public async Task Save_generates_markdown_persists_and_reloads()
    {
        await using var db = await TestDatabase.CreateAsync();
        var service = BuildService(db, out var systemRepo);
        var systemId = await systemRepo.CreateAsync(new SystemEntry { Name = "Web Application" });

        var aggregate = new ReleasePlanAggregate
        {
            Plan = new ReleasePlan
            {
                Title = "June Release",
                ReleaseDate = "2026-06-14",
                Environment = Environments.Production,
                CreatedBy = "Eduardo",
                TemplateId = 1, // seeded default release template
                DeploymentSteps = "Stop app\nDeploy\nStart app",
            },
        };
        aggregate.Tickets.Add(new ReleasePlanTicket { TicketNumber = "JIRA-100", TicketName = "Add feature" });
        aggregate.Systems.Add(new ReleasePlanSystemLink { SystemEntryId = systemId });

        var (saved, id, validation) = await service.SaveAsync(aggregate);

        Assert.True(saved, string.Join("; ", validation.Errors));
        Assert.True(id > 0);

        var reloaded = await service.GetAggregateAsync(id);
        Assert.NotNull(reloaded);
        Assert.False(string.IsNullOrWhiteSpace(reloaded!.Plan.MarkdownOutput));
        Assert.Contains("**Release Date:** 2026-06-14", reloaded.Plan.MarkdownOutput);
        Assert.Contains("## JIRA-100 Add feature", reloaded.Plan.MarkdownOutput);
        Assert.Contains("- Web Application", reloaded.Plan.MarkdownOutput);
        Assert.Contains("1. Stop app", reloaded.Plan.MarkdownOutput);
    }

    [Fact]
    public async Task Save_returns_validation_errors_for_incomplete_plan()
    {
        await using var db = await TestDatabase.CreateAsync();
        var service = BuildService(db, out _);

        var (saved, _, validation) = await service.SaveAsync(new ReleasePlanAggregate
        {
            Plan = new ReleasePlan { Title = "Incomplete" },
        });

        Assert.False(saved);
        Assert.NotEmpty(validation.Errors);
    }
}
