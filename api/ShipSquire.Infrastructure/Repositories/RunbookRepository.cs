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

    public async Task<Runbook?> GetLatestForServiceAsync(Guid serviceId, CancellationToken cancellationToken = default)
    {
        // First try to find the latest published runbook
        var published = await _dbSet
            .Where(r => r.ServiceId == serviceId && r.Status == "published")
            .OrderByDescending(r => r.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (published != null)
            return published;

        // Fallback to the latest draft runbook
        return await _dbSet
            .Where(r => r.ServiceId == serviceId)
            .OrderByDescending(r => r.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
