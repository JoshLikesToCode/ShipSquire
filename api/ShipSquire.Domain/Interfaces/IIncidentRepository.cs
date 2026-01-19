using ShipSquire.Domain.Entities;

namespace ShipSquire.Domain.Interfaces;

public interface IIncidentRepository : IRepository<Incident>
{
    Task<IEnumerable<Incident>> GetByServiceIdAsync(Guid serviceId, CancellationToken cancellationToken = default);
    Task<Incident?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Incident?> GetByIdAndUserIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
}
