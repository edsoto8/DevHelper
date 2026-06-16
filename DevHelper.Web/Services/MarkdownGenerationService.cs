using System.Text;
using System.Text.RegularExpressions;
using DevHelper.Web.Models;

namespace DevHelper.Web.Services;

/// <inheritdoc />
public sealed partial class MarkdownGenerationService : IMarkdownGenerationService
{
    private readonly ILogger<MarkdownGenerationService> _logger;

    public MarkdownGenerationService(ILogger<MarkdownGenerationService> logger)
    {
        _logger = logger;
    }

    [GeneratedRegex(@"\{\{(?<name>[A-Za-z0-9_]+)\}\}")]
    private static partial Regex PlaceholderRegex();

    public string GenerateReleasePlanMarkdown(string template, ReleasePlanRenderModel model)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Title"] = MarkdownRenderHelpers.Scalar(model.Title),
            ["ReleaseDate"] = MarkdownRenderHelpers.FormatDate(model.ReleaseDate),
            ["Environment"] = MarkdownRenderHelpers.Scalar(model.Environment),
            ["CreatedBy"] = MarkdownRenderHelpers.Scalar(model.CreatedBy),
            ["Tickets"] = RenderTickets(model.Tickets),
            ["Systems"] = RenderSystems(model.SystemNames),
            ["Servers"] = RenderServers(model.Servers),
            ["Databases"] = RenderDatabases(model.Databases),
            ["SqlScripts"] = RenderSqlScripts(model.SqlScripts),
            ["BackupSteps"] = MarkdownRenderHelpers.NumberedListFromText(model.BackupSteps),
            ["DeploymentSteps"] = MarkdownRenderHelpers.NumberedListFromText(model.DeploymentSteps),
            ["ValidationSteps"] = MarkdownRenderHelpers.NumberedListFromText(model.ValidationSteps),
            ["RollbackSteps"] = MarkdownRenderHelpers.NumberedListFromText(model.RollbackSteps),
            ["Screenshots"] = RenderScreenshots(model.Screenshots),
            ["Notes"] = MarkdownRenderHelpers.Scalar(model.Notes),
        };

        return Replace(template, values);
    }

    /// <summary>Replaces known placeholders; leaves unknown ones and logs a warning.</summary>
    private string Replace(string template, IReadOnlyDictionary<string, string> values)
    {
        return PlaceholderRegex().Replace(template, match =>
        {
            var name = match.Groups["name"].Value;
            if (values.TryGetValue(name, out var value))
            {
                return value;
            }

            _logger.LogWarning("Unknown template placeholder {{{{{Placeholder}}}}} left unchanged.", name);
            return match.Value;
        });
    }

    private static string RenderTickets(IEnumerable<TicketRenderItem> tickets)
    {
        var ordered = tickets.OrderBy(t => t.SortOrder).ThenBy(t => t.Id).ToList();
        if (ordered.Count == 0)
        {
            return MarkdownRenderHelpers.EmptyCollectionText;
        }

        var blocks = new List<string>();
        foreach (var ticket in ordered)
        {
            var heading = $"## {MarkdownRenderHelpers.EscapeInline(ticket.TicketNumber)} {MarkdownRenderHelpers.EscapeInline(ticket.TicketName)}".TrimEnd();
            if (!string.IsNullOrWhiteSpace(ticket.Summary))
            {
                blocks.Add($"{heading}\n\n{ticket.Summary}");
            }
            else
            {
                blocks.Add(heading);
            }
        }

        return string.Join("\n\n", blocks);
    }

    private static string RenderSystems(IEnumerable<string> systemNames)
    {
        var names = systemNames.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
        if (names.Count == 0)
        {
            return MarkdownRenderHelpers.EmptyCollectionText;
        }

        return string.Join("\n", names.Select(n => $"- {MarkdownRenderHelpers.EscapeInline(n)}"));
    }

    private static string RenderServers(IEnumerable<ServerRenderItem> servers)
    {
        var ordered = servers.OrderBy(s => s.SortOrder).ThenBy(s => s.Id).ToList();
        if (ordered.Count == 0)
        {
            return MarkdownRenderHelpers.EmptyCollectionText;
        }

        return string.Join("\n", ordered.Select(s =>
            $"- {MarkdownRenderHelpers.EscapeInline(s.Name)} - {MarkdownRenderHelpers.EscapeInline(s.Environment)} - {MarkdownRenderHelpers.EscapeInline(s.ServerType)}"));
    }

    private static string RenderDatabases(IEnumerable<DatabaseRenderItem> databases)
    {
        var ordered = databases.OrderBy(d => d.SortOrder).ThenBy(d => d.Id).ToList();
        if (ordered.Count == 0)
        {
            return MarkdownRenderHelpers.EmptyCollectionText;
        }

        return string.Join("\n", ordered.Select(d =>
            $"- {MarkdownRenderHelpers.EscapeInline(d.Name)} on {MarkdownRenderHelpers.EscapeInline(d.SqlServerInstance)}"));
    }

    private static string RenderSqlScripts(IEnumerable<SqlScriptRenderItem> scripts)
    {
        var ordered = scripts.OrderBy(s => s.ExecutionOrder).ThenBy(s => s.Id).ToList();
        if (ordered.Count == 0)
        {
            return MarkdownRenderHelpers.EmptyCollectionText;
        }

        var builder = new StringBuilder();
        builder.AppendLine("| Order | Database | Script Name | Required | Description |");
        builder.Append("|---|---|---|---|---|");
        foreach (var script in ordered)
        {
            builder.Append('\n');
            builder.Append("| ")
                .Append(script.ExecutionOrder).Append(" | ")
                .Append(MarkdownRenderHelpers.EscapeInline(script.DatabaseName)).Append(" | ")
                .Append(MarkdownRenderHelpers.EscapeInline(script.ScriptName)).Append(" | ")
                .Append(script.IsRequired ? "Yes" : "No").Append(" | ")
                .Append(MarkdownRenderHelpers.EscapeInline(script.Description)).Append(" |");
        }

        return builder.ToString();
    }

    private string RenderScreenshots(IEnumerable<ScreenshotRenderItem> screenshots)
    {
        var ordered = screenshots.OrderBy(s => s.SortOrder).ThenBy(s => s.Id).ToList();
        if (ordered.Count == 0)
        {
            return MarkdownRenderHelpers.EmptyCollectionText;
        }

        var lines = new List<string>();
        foreach (var shot in ordered)
        {
            var description = MarkdownRenderHelpers.EscapeInline(shot.Description);
            if (shot.FileExists)
            {
                lines.Add($"![{description}]({shot.FilePath})");
            }
            else
            {
                _logger.LogWarning("Screenshot file missing during Markdown generation: {FilePath}.", shot.FilePath);
                lines.Add($"> ⚠️ Missing screenshot file: {shot.FilePath}");
            }
        }

        return string.Join("\n\n", lines);
    }
}
