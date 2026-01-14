using ShipSquire.Domain.Entities;

namespace ShipSquire.Domain.Interfaces;

public interface IServiceRepository : IRepository<Service>
{
    Task<IEnumerable<Service>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Service?> GetByIdAndUserIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<Service?> GetByIdWithUserAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
}
