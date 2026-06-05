namespace ReleasePlanGenerator.Web.Models;

public class ReleasePlanTicket
{
    public int Id { get; set; }
    public int ReleasePlanId { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string TicketName { get; set; } = string.Empty;
    public string? TicketSummary { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
