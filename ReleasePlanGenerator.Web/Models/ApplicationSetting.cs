namespace ReleasePlanGenerator.Web.Models;

public class ApplicationSetting
{
    public int Id { get; set; }
    public string SettingKey { get; set; } = string.Empty;
    public string? SettingValue { get; set; }
    public bool IsEncrypted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public static class SettingKeys
{
    public const string TicketLookupSqlServerConnectionString = "TicketLookupSqlServerConnectionString";
    public const string DefaultTemplateId = "DefaultTemplateId";
    public const string DefaultEnvironment = "DefaultEnvironment";
    public const string PdfOutputPath = "PdfOutputPath";
    public const string MarkdownOutputPath = "MarkdownOutputPath";
    public const string LogLevel = "LogLevel";
}
