using Microsoft.EntityFrameworkCore;
using ShipSquire.Domain.Entities;
using ShipSquire.Domain.Interfaces;
using ShipSquire.Infrastructure.Persistence;

namespace ShipSquire.Infrastructure.Repositories;

public class TimelineEntryRepository : Repository<IncidentTimelineEntry>, ITimelineEntryRepository
{
    public TimelineEntryRepository(ShipSquireDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<IncidentTimelineEntry>> GetByIncidentIdAsync(Guid incidentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(e => e.IncidentId == incidentId)
            .OrderBy(e => e.OccurredAt)
            .ThenBy(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
