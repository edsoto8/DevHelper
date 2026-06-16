using DevHelper.Web.Models;
using DevHelper.Web.Services;

namespace DevHelper.Tests;

public sealed class MarkdownGenerationTests
{
    private static MarkdownGenerationService Service(out ListLogger<MarkdownGenerationService> logger)
    {
        logger = new ListLogger<MarkdownGenerationService>();
        return new MarkdownGenerationService(logger);
    }

    [Fact]
    public void Replaces_scalar_placeholders_case_sensitively()
    {
        var service = Service(out _);
        var model = new ReleasePlanRenderModel
        {
            Title = "My Release",
            ReleaseDate = "2026-06-14",
            Environment = "Production",
            CreatedBy = "Eduardo",
        };

        var output = service.GenerateReleasePlanMarkdown(
            "T={{Title}} E={{Environment}} B={{CreatedBy}}", model);

        Assert.Equal("T=My Release E=Production B=Eduardo", output);
    }

    [Fact]
    public void Formats_release_date_as_yyyy_MM_dd()
    {
        var service = Service(out _);
        var model = new ReleasePlanRenderModel { ReleaseDate = "2026-06-14T10:30:00Z" };

        var output = service.GenerateReleasePlanMarkdown("{{ReleaseDate}}", model);

        Assert.Equal("2026-06-14", output);
    }

    [Fact]
    public void Empty_collections_render_as_None()
    {
        var service = Service(out _);
        var output = service.GenerateReleasePlanMarkdown(
            "{{Tickets}}|{{Systems}}|{{Servers}}|{{Databases}}|{{SqlScripts}}|{{BackupSteps}}",
            new ReleasePlanRenderModel());

        Assert.Equal("None|None|None|None|None|None", output);
    }

    [Fact]
    public void Tickets_render_with_heading_and_summary_in_order()
    {
        var service = Service(out _);
        var model = new ReleasePlanRenderModel
        {
            Tickets =
            {
                new TicketRenderItem { Id = 2, SortOrder = 1, TicketNumber = "T-2", TicketName = "Second" },
                new TicketRenderItem { Id = 1, SortOrder = 0, TicketNumber = "T-1", TicketName = "First", Summary = "Does a thing." },
            },
        };

        var output = service.GenerateReleasePlanMarkdown("{{Tickets}}", model);

        Assert.Equal("## T-1 First\n\nDoes a thing.\n\n## T-2 Second", output);
    }

    [Fact]
    public void Servers_and_databases_use_specified_format()
    {
        var service = Service(out _);
        var model = new ReleasePlanRenderModel
        {
            Servers = { new ServerRenderItem { Name = "WEB01", Environment = "Production", ServerType = "Web" } },
            Databases = { new DatabaseRenderItem { Name = "AppDb", SqlServerInstance = "SQL01" } },
        };

        Assert.Equal("- WEB01 - Production - Web",
            service.GenerateReleasePlanMarkdown("{{Servers}}", model));
        Assert.Equal("- AppDb on SQL01",
            service.GenerateReleasePlanMarkdown("{{Databases}}", model));
    }

    [Fact]
    public void SqlScripts_render_as_table_and_escape_pipes()
    {
        var service = Service(out _);
        var model = new ReleasePlanRenderModel
        {
            SqlScripts =
            {
                new SqlScriptRenderItem
                {
                    ExecutionOrder = 1, DatabaseName = "AppDb", ScriptName = "a|b.sql",
                    IsRequired = true, Description = "first",
                },
            },
        };

        var output = service.GenerateReleasePlanMarkdown("{{SqlScripts}}", model);

        Assert.Equal(
            "| Order | Database | Script Name | Required | Description |\n" +
            "|---|---|---|---|---|\n" +
            "| 1 | AppDb | a\\|b.sql | Yes | first |",
            output);
    }

    [Fact]
    public void Step_text_splits_into_numbered_list()
    {
        var service = Service(out _);
        var model = new ReleasePlanRenderModel { DeploymentSteps = "Stop app\n\nDeploy\nStart app" };

        var output = service.GenerateReleasePlanMarkdown("{{DeploymentSteps}}", model);

        Assert.Equal("1. Stop app\n2. Deploy\n3. Start app", output);
    }

    [Fact]
    public void Screenshots_render_image_reference_or_missing_marker()
    {
        var service = Service(out var logger);
        var model = new ReleasePlanRenderModel
        {
            Screenshots =
            {
                new ScreenshotRenderItem { Description = "ok", FilePath = "a.png", FileExists = true },
                new ScreenshotRenderItem { Description = "gone", FilePath = "b.png", FileExists = false },
            },
        };

        var output = service.GenerateReleasePlanMarkdown("{{Screenshots}}", model);

        Assert.Contains("![ok](a.png)", output);
        Assert.Contains("Missing screenshot file: b.png", output);
        Assert.Contains(logger.Entries, e => e.Level == Microsoft.Extensions.Logging.LogLevel.Warning);
    }

    [Fact]
    public void Unknown_placeholder_is_left_unchanged_and_warns()
    {
        var service = Service(out var logger);

        var output = service.GenerateReleasePlanMarkdown("Hello {{Unknown}} {{Title}}",
            new ReleasePlanRenderModel { Title = "X" });

        Assert.Equal("Hello {{Unknown}} X", output);
        Assert.Contains(logger.Entries, e =>
            e.Level == Microsoft.Extensions.Logging.LogLevel.Warning && e.Message.Contains("Unknown"));
    }

    [Fact]
    public void Default_template_renders_full_document()
    {
        var service = Service(out _);
        var template =
            "# Release Plan\n\n**Release Date:** {{ReleaseDate}}\n**Environment:** {{Environment}}\n\n{{Tickets}}\n\n{{Systems}}\n\n{{Notes}}";
        var model = new ReleasePlanRenderModel
        {
            ReleaseDate = "2026-06-14",
            Environment = "QA",
            Tickets = { new TicketRenderItem { TicketNumber = "T-1", TicketName = "Work" } },
            SystemNames = { "Web", "API" },
            Notes = "All good",
        };

        var output = service.GenerateReleasePlanMarkdown(template, model);

        Assert.Contains("**Release Date:** 2026-06-14", output);
        Assert.Contains("## T-1 Work", output);
        Assert.Contains("- Web\n- API", output);
        Assert.Contains("All good", output);
    }
}
