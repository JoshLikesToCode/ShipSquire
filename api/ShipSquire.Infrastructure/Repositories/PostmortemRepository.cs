using Microsoft.EntityFrameworkCore;
using ShipSquire.Domain.Entities;
using ShipSquire.Domain.Interfaces;
using ShipSquire.Infrastructure.Persistence;

namespace ShipSquire.Infrastructure.Repositories;

public class PostmortemRepository : Repository<Postmortem>, IPostmortemRepository
{
    public PostmortemRepository(ShipSquireDbContext context) : base(context)
    {
    }

    public async Task<Postmortem?> GetByIncidentIdAsync(Guid incidentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(p => p.IncidentId == incidentId, cancellationToken);
    }
}
