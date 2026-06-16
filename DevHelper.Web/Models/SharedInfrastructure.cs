namespace DevHelper.Web.Models;

/// <summary>A known system. Named SystemEntry to avoid colliding with System namespace.</summary>
public sealed class SystemEntry
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
}

public sealed class ServerEntry
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public long? SystemEntryId { get; set; }
    public string ServerType { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
}

public sealed class DatabaseEntry
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SqlServerInstance { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public long? SystemEntryId { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
}

public sealed class Template
{
    public long Id { get; set; }
    public string ToolType { get; set; } = Models.ToolType.ReleasePlan.ToString();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string MarkdownTemplate { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
}

public sealed class ApplicationSetting
{
    public long Id { get; set; }
    public string SettingKey { get; set; } = string.Empty;
    public string? SettingValue { get; set; }
    public bool IsEncrypted { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
}

/// <summary>A screenshot attachment reference (metadata only; never the image bytes).</summary>
public sealed class PlanScreenshot
{
    public long Id { get; set; }
    public string PlanType { get; set; } = PlanTypes.ReleasePlan;
    public long PlanId { get; set; }
    public string? Description { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string AttachedAt { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
}
