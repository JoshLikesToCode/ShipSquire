using ShipSquire.Domain.Common;

namespace ShipSquire.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? AuthProvider { get; set; }

    // Navigation properties
    public ICollection<Service> Services { get; set; } = new List<Service>();
    public ICollection<Runbook> Runbooks { get; set; } = new List<Runbook>();
    public ICollection<Incident> Incidents { get; set; } = new List<Incident>();
}
