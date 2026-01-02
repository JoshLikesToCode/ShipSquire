using ShipSquire.Domain.Common;

namespace ShipSquire.Domain.Entities;

public class RunbookSection : BaseEntity
{
    public Guid RunbookId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Order { get; set; }
    public string BodyMarkdown { get; set; } = string.Empty;

    // Navigation properties
    public Runbook Runbook { get; set; } = null!;
}
