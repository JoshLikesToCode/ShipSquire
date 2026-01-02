using ShipSquire.Domain.Common;

namespace ShipSquire.Domain.Entities;

public class IncidentTimelineEntry : BaseEntity
{
    public Guid IncidentId { get; set; }
    public string EntryType { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
    public string BodyMarkdown { get; set; } = string.Empty;

    // Navigation properties
    public Incident Incident { get; set; } = null!;
}
