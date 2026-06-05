namespace ReleasePlanGenerator.Web.Models;

public class SqlScript
{
    public int Id { get; set; }
    public int ReleasePlanId { get; set; }
    public int? DatabaseEntryId { get; set; }
    public string ScriptName { get; set; } = string.Empty;
    public string? ScriptDescription { get; set; }
    public int ExecutionOrder { get; set; }
    public bool IsRequired { get; set; } = true;
    public string? Notes { get; set; }

    public string? DatabaseName { get; set; }
}
