namespace ReleasePlanGenerator.Web.Models;

public class ReleasePlan
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime ReleaseDate { get; set; }
    public string Environment { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public int? TemplateId { get; set; }
    public string? MarkdownOutput { get; set; }
    public string? PdfFilePath { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<ReleasePlanTicket> Tickets { get; set; } = [];
    public List<SqlScript> SqlScripts { get; set; } = [];
    public List<int> SelectedSystemIds { get; set; } = [];
    public List<int> SelectedServerIds { get; set; } = [];
    public List<int> SelectedDatabaseIds { get; set; } = [];
    public string? DeploymentSteps { get; set; }
    public string? ValidationSteps { get; set; }
    public string? RollbackSteps { get; set; }
    public string? Notes { get; set; }
}
