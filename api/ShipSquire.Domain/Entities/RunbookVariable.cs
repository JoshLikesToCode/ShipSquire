using ShipSquire.Domain.Common;

namespace ShipSquire.Domain.Entities;

public class RunbookVariable : BaseEntity
{
    public Guid RunbookId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ValueHint { get; set; }
    public bool IsSecret { get; set; }
    public string? Description { get; set; }

    // Navigation properties
    public Runbook Runbook { get; set; } = null!;
}
