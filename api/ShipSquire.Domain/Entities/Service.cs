using ShipSquire.Domain.Common;

namespace ShipSquire.Domain.Entities;

public class Service : BaseEntity
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? RepoProvider { get; set; }
    public string? RepoOwner { get; set; }
    public string? RepoName { get; set; }
    public string? RepoUrl { get; set; }
    public string? DefaultBranch { get; set; }
    public string? PrimaryLanguage { get; set; }
    public string? Tags { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<Runbook> Runbooks { get; set; } = new List<Runbook>();
    public ICollection<Incident> Incidents { get; set; } = new List<Incident>();
}
