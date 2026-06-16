namespace DevHelper.Web.Models;

/// <summary>
/// Fully-resolved data passed to the Markdown generator for a release plan. All
/// foreign keys have already been resolved to display values so the generator
/// stays pure and easily testable.
/// </summary>
public sealed class ReleasePlanRenderModel
{
    public string Title { get; set; } = string.Empty;
    public string ReleaseDate { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public string? BackupSteps { get; set; }
    public string? DeploymentSteps { get; set; }
    public string? ValidationSteps { get; set; }
    public string? RollbackSteps { get; set; }
    public string? Notes { get; set; }

    public List<TicketRenderItem> Tickets { get; set; } = new();
    public List<string> SystemNames { get; set; } = new();
    public List<ServerRenderItem> Servers { get; set; } = new();
    public List<DatabaseRenderItem> Databases { get; set; } = new();
    public List<SqlScriptRenderItem> SqlScripts { get; set; } = new();
    public List<ScreenshotRenderItem> Screenshots { get; set; } = new();
}

public sealed class TicketRenderItem
{
    public string TicketNumber { get; set; } = string.Empty;
    public string TicketName { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public int SortOrder { get; set; }
    public long Id { get; set; }
}

public sealed class ServerRenderItem
{
    public string Name { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public string ServerType { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public long Id { get; set; }
}

public sealed class DatabaseRenderItem
{
    public string Name { get; set; } = string.Empty;
    public string SqlServerInstance { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public long Id { get; set; }
}

public sealed class SqlScriptRenderItem
{
    public int ExecutionOrder { get; set; }
    public string DatabaseName { get; set; } = string.Empty;
    public string ScriptName { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public string? Description { get; set; }
    public long Id { get; set; }
}

public sealed class ScreenshotRenderItem
{
    public string? Description { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public bool FileExists { get; set; } = true;
    public int SortOrder { get; set; }
    public long Id { get; set; }
}
