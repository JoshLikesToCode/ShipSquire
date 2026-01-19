using ShipSquire.Domain.Entities;

namespace ShipSquire.Domain.Interfaces;

public interface IRunbookRepository : IRepository<Runbook>
{
    Task<IEnumerable<Runbook>> GetByServiceIdAsync(Guid serviceId, CancellationToken cancellationToken = default);
    Task<Runbook?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Runbook?> GetByIdAndUserIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<Runbook?> GetLatestForServiceAsync(Guid serviceId, CancellationToken cancellationToken = default);
}
