namespace ReleasePlanGenerator.Web.Models;

public class DatabaseEntry
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SqlServerInstance { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public int? SystemEntryId { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
