using ReleasePlanGenerator.Web.Models;
using ReleasePlanGenerator.Web.Repositories;
using Serilog;
using System.Text;

namespace ReleasePlanGenerator.Web.Services;

public class MarkdownGenerationService(
    IReleaseTemplateRepository templateRepository,
    ISystemRepository systemRepository,
    IServerRepository serverRepository,
    IDatabaseRepository databaseRepository) : IMarkdownGenerationService
{
    public async Task<string> GenerateAsync(ReleasePlan plan)
    {
        try
        {
            var template = plan.TemplateId.HasValue
                ? await templateRepository.GetByIdAsync(plan.TemplateId.Value)
                : await templateRepository.GetDefaultAsync();

            var markdown = template?.MarkdownTemplate ?? GetFallbackTemplate();

            var systems = plan.SelectedSystemIds.Count > 0
                ? (await systemRepository.GetActiveAsync()).Where(s => plan.SelectedSystemIds.Contains(s.Id)).ToList()
                : [];

            var servers = plan.SelectedServerIds.Count > 0
                ? (await serverRepository.GetActiveAsync()).Where(s => plan.SelectedServerIds.Contains(s.Id)).ToList()
                : [];

            var databases = plan.SelectedDatabaseIds.Count > 0
                ? (await databaseRepository.GetActiveAsync()).Where(d => plan.SelectedDatabaseIds.Contains(d.Id)).ToList()
                : [];

            markdown = markdown
                .Replace("{{ReleaseDate}}", plan.ReleaseDate.ToString("yyyy-MM-dd"))
                .Replace("{{Environment}}", plan.Environment)
                .Replace("{{CreatedBy}}", plan.CreatedBy)
                .Replace("{{Tickets}}", BuildTicketsSection(plan.Tickets))
                .Replace("{{Systems}}", BuildListSection(systems.Select(s => s.Name)))
                .Replace("{{Servers}}", BuildListSection(servers.Select(s => $"{s.Name} - {s.Environment} - {s.ServerType}")))
                .Replace("{{Databases}}", BuildListSection(databases.Select(d => $"{d.Name} on {d.SqlServerInstance}")))
                .Replace("{{SqlScripts}}", BuildSqlScriptsSection(plan.SqlScripts, databases.ToDictionary(d => d.Id, d => d.Name)))
                .Replace("{{DeploymentSteps}}", FormatSteps(plan.DeploymentSteps))
                .Replace("{{ValidationSteps}}", FormatSteps(plan.ValidationSteps))
                .Replace("{{RollbackSteps}}", FormatSteps(plan.RollbackSteps))
                .Replace("{{Notes}}", plan.Notes ?? "_No additional notes._");

            return markdown;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Markdown generation failed for ReleasePlanId {ReleasePlanId}", plan.Id);
            throw;
        }
    }

    private static string BuildTicketsSection(List<ReleasePlanTicket> tickets)
    {
        if (tickets.Count == 0) return "_No tickets added._";

        var sb = new StringBuilder();
        foreach (var t in tickets.OrderBy(t => t.SortOrder))
        {
            sb.AppendLine($"# {t.TicketNumber} {t.TicketName}");
            sb.AppendLine();
            sb.AppendLine("## Summary");
            sb.AppendLine();
            sb.AppendLine(string.IsNullOrWhiteSpace(t.TicketSummary) ? "_No summary provided._" : t.TicketSummary);
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }
        return sb.ToString().TrimEnd();
    }

    private static string BuildListSection(IEnumerable<string> items)
    {
        var list = items.ToList();
        return list.Count == 0
            ? "_None selected._"
            : string.Join("\n", list.Select(i => $"- {i}"));
    }

    private static string BuildSqlScriptsSection(List<SqlScript> scripts, Dictionary<int, string> dbNames)
    {
        if (scripts.Count == 0) return "_No SQL scripts._";

        var sb = new StringBuilder();
        sb.AppendLine("| Order | Database | Script Name | Required |");
        sb.AppendLine("|---|---|---|---|");
        foreach (var s in scripts.OrderBy(s => s.ExecutionOrder))
        {
            var dbName = s.DatabaseEntryId.HasValue && dbNames.TryGetValue(s.DatabaseEntryId.Value, out var n) ? n : "-";
            sb.AppendLine($"| {s.ExecutionOrder} | {dbName} | {s.ScriptName} | {(s.IsRequired ? "Yes" : "No")} |");
        }
        return sb.ToString().TrimEnd();
    }

    private static string FormatSteps(string? steps)
    {
        if (string.IsNullOrWhiteSpace(steps)) return "_No steps provided._";
        var lines = steps.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var sb = new StringBuilder();
        for (var i = 0; i < lines.Length; i++)
            sb.AppendLine($"{i + 1}. {lines[i].TrimStart('-', ' ')}");
        return sb.ToString().TrimEnd();
    }

    private static string GetFallbackTemplate() => """
        # Release Plan

        ## Release Information

        **Release Date:** {{ReleaseDate}}
        **Environment:** {{Environment}}
        **Created By:** {{CreatedBy}}

        ---

        # Tickets

        {{Tickets}}

        ---

        # Affected Systems

        {{Systems}}

        ---

        # Servers

        {{Servers}}

        ---

        # Databases

        {{Databases}}

        ---

        # Deployment Steps

        {{DeploymentSteps}}

        ---

        # Execute SQL Scripts

        {{SqlScripts}}

        ---

        # Validation

        {{ValidationSteps}}

        ---

        # Rollback Plan

        {{RollbackSteps}}

        ---

        # Notes

        {{Notes}}
        """;
}
