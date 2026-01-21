using ShipSquire.Domain.Entities;

namespace ShipSquire.Domain.Interfaces;

public interface IPostmortemRepository : IRepository<Postmortem>
{
    Task<Postmortem?> GetByIncidentIdAsync(Guid incidentId, CancellationToken cancellationToken = default);
}
