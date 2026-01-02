using ShipSquire.Domain.Common;

namespace ShipSquire.Domain.Entities;

public class Postmortem : BaseEntity
{
    public Guid IncidentId { get; set; }
    public string? ImpactMarkdown { get; set; }
    public string? RootCauseMarkdown { get; set; }
    public string? DetectionMarkdown { get; set; }
    public string? ResolutionMarkdown { get; set; }
    public string? ActionItemsMarkdown { get; set; }

    // Navigation properties
    public Incident Incident { get; set; } = null!;
}
