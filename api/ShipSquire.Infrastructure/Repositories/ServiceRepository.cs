using Microsoft.EntityFrameworkCore;
using ShipSquire.Domain.Entities;
using ShipSquire.Domain.Interfaces;
using ShipSquire.Infrastructure.Persistence;

namespace ShipSquire.Infrastructure.Repositories;

public class ServiceRepository : Repository<Service>, IServiceRepository
{
    public ServiceRepository(ShipSquireDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Service>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(s => s.UserId == userId)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Service?> GetByIdAndUserIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId, cancellationToken);
    }

    public async Task<Service?> GetByIdWithUserAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId, cancellationToken);
    }
}
