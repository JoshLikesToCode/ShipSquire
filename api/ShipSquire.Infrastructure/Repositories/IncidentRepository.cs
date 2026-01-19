using Microsoft.EntityFrameworkCore;
using ShipSquire.Domain.Entities;
using ShipSquire.Domain.Interfaces;
using ShipSquire.Infrastructure.Persistence;

namespace ShipSquire.Infrastructure.Repositories;

public class IncidentRepository : Repository<Incident>, IIncidentRepository
{
    public IncidentRepository(ShipSquireDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Incident>> GetByServiceIdAsync(Guid serviceId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(i => i.ServiceId == serviceId)
            .Include(i => i.Runbook)
            .OrderByDescending(i => i.StartedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Incident?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(i => i.Runbook)
            .Include(i => i.Service)
            .Include(i => i.TimelineEntries)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<Incident?> GetByIdAndUserIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId, cancellationToken);
    }
}
