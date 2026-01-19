using ShipSquire.Domain.Entities;

namespace ShipSquire.Domain.Interfaces;

public interface ITimelineEntryRepository : IRepository<IncidentTimelineEntry>
{
    Task<IEnumerable<IncidentTimelineEntry>> GetByIncidentIdAsync(Guid incidentId, CancellationToken cancellationToken = default);
}
