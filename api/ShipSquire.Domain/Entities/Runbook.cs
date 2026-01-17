using ShipSquire.Domain.Common;

namespace ShipSquire.Domain.Entities;

public class Runbook : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid ServiceId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = "draft";
    public int Version { get; set; } = 1;
    public string? Summary { get; set; }
    public string Origin { get; set; } = "manual";  // "manual" or "generated"
    public string? AnalysisSnapshot { get; set; }   // JSON of RepoAnalysisResult

    // Navigation properties
    public User User { get; set; } = null!;
    public Service Service { get; set; } = null!;
    public ICollection<RunbookSection> Sections { get; set; } = new List<RunbookSection>();
    public ICollection<RunbookVariable> Variables { get; set; } = new List<RunbookVariable>();
    public ICollection<Incident> Incidents { get; set; } = new List<Incident>();
}
