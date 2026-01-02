using ShipSquire.Domain.Common;

namespace ShipSquire.Domain.Entities;

public class Incident : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid ServiceId { get; set; }
    public Guid? RunbookId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Severity { get; set; } = "sev3";
    public string Status { get; set; } = "open";
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
    public string? SummaryMarkdown { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public Service Service { get; set; } = null!;
    public Runbook? Runbook { get; set; }
    public ICollection<IncidentTimelineEntry> TimelineEntries { get; set; } = new List<IncidentTimelineEntry>();
    public Postmortem? Postmortem { get; set; }
}
