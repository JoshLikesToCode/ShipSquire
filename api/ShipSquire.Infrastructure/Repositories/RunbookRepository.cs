using Microsoft.EntityFrameworkCore;
using ShipSquire.Domain.Entities;
using ShipSquire.Domain.Interfaces;
using ShipSquire.Infrastructure.Persistence;

namespace ShipSquire.Infrastructure.Repositories;

public class RunbookRepository : Repository<Runbook>, IRunbookRepository
{
    public RunbookRepository(ShipSquireDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Runbook>> GetByServiceIdAsync(Guid serviceId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.ServiceId == serviceId)
            .OrderBy(r => r.Title)
            .ToListAsync(cancellationToken);
    }

    public async Task<Runbook?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(r => r.Sections)
            .Include(r => r.Variables)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<Runbook?> GetByIdAndUserIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId, cancellationToken);
    }
}
