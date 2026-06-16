namespace DevHelper.Web.Models;

public sealed class ReleasePlan
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ReleaseDate { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public long? TemplateId { get; set; }
    public string? BackupSteps { get; set; }
    public string? DeploymentSteps { get; set; }
    public string? ValidationSteps { get; set; }
    public string? RollbackSteps { get; set; }
    public string? Notes { get; set; }
    public string? MarkdownOutput { get; set; }
    public string? PdfFilePath { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
}

public sealed class ReleasePlanTicket
{
    public long Id { get; set; }
    public long ReleasePlanId { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string TicketName { get; set; } = string.Empty;
    public string? TicketSummary { get; set; }
    public int SortOrder { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
}

public sealed class SqlScript
{
    public long Id { get; set; }
    public long ReleasePlanId { get; set; }
    public long? DatabaseEntryId { get; set; }
    public string ScriptName { get; set; } = string.Empty;
    public string? ScriptDescription { get; set; }
    public int ExecutionOrder { get; set; } = 1;
    public bool IsRequired { get; set; } = true;
    public string? Notes { get; set; }
}

public sealed class ReleasePlanSystemLink
{
    public long Id { get; set; }
    public long ReleasePlanId { get; set; }
    public long? SystemEntryId { get; set; }
    public string? OtherSystemName { get; set; }
    public int SortOrder { get; set; }
}

public sealed class ReleasePlanServerLink
{
    public long Id { get; set; }
    public long ReleasePlanId { get; set; }
    public long ServerEntryId { get; set; }
    public int SortOrder { get; set; }
}

public sealed class ReleasePlanDatabaseLink
{
    public long Id { get; set; }
    public long ReleasePlanId { get; set; }
    public long DatabaseEntryId { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>
/// A release plan together with all of its child records. This is the unit the
/// service layer validates and the repository persists transactionally.
/// </summary>
public sealed class ReleasePlanAggregate
{
    public ReleasePlan Plan { get; set; } = new();
    public List<ReleasePlanTicket> Tickets { get; set; } = new();
    public List<SqlScript> SqlScripts { get; set; } = new();
    public List<ReleasePlanSystemLink> Systems { get; set; } = new();
    public List<ReleasePlanServerLink> Servers { get; set; } = new();
    public List<ReleasePlanDatabaseLink> Databases { get; set; } = new();
    public List<PlanScreenshot> Screenshots { get; set; } = new();
}
