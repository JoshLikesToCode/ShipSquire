using ShipSquire.Application.DTOs;
using ShipSquire.Application.Interfaces;
using ShipSquire.Domain.Entities;
using ShipSquire.Domain.Enums;
using ShipSquire.Domain.Interfaces;

namespace ShipSquire.Application.Services;

public class TimelineEntryService
{
    private readonly ITimelineEntryRepository _timelineEntryRepository;
    private readonly IIncidentRepository _incidentRepository;
    private readonly ICurrentUser _currentUser;

    public TimelineEntryService(
        ITimelineEntryRepository timelineEntryRepository,
        IIncidentRepository incidentRepository,
        ICurrentUser currentUser)
    {
        _timelineEntryRepository = timelineEntryRepository;
        _incidentRepository = incidentRepository;
        _currentUser = currentUser;
    }

    public async Task<IEnumerable<TimelineEntryResponse>> GetByIncidentIdAsync(Guid incidentId, CancellationToken cancellationToken = default)
    {
        // Verify incident ownership
        var incident = await _incidentRepository.GetByIdAndUserIdAsync(incidentId, _currentUser.UserId, cancellationToken);
        if (incident == null) return Enumerable.Empty<TimelineEntryResponse>();

        var entries = await _timelineEntryRepository.GetByIncidentIdAsync(incidentId, cancellationToken);
        return entries.Select(MapToResponse);
    }

    public async Task<TimelineEntryResponse?> AddEntryAsync(Guid incidentId, TimelineEntryRequest request, CancellationToken cancellationToken = default)
    {
        // Verify incident ownership
        var incident = await _incidentRepository.GetByIdAndUserIdAsync(incidentId, _currentUser.UserId, cancellationToken);
        if (incident == null) return null;

        // Validate entry type
        if (!TimelineEntryType.IsValid(request.EntryType))
        {
            throw new ArgumentException($"Invalid entry type. Must be one of: {string.Join(", ", TimelineEntryType.All)}");
        }

        // Validate body is not empty
        if (string.IsNullOrWhiteSpace(request.BodyMarkdown))
        {
            throw new ArgumentException("Body markdown is required");
        }

        var entry = new IncidentTimelineEntry
        {
            IncidentId = incidentId,
            EntryType = request.EntryType,
            OccurredAt = DateTimeOffset.UtcNow,  // Server-side timestamp
            BodyMarkdown = request.BodyMarkdown
        };

        await _timelineEntryRepository.AddAsync(entry, cancellationToken);

        return MapToResponse(entry);
    }

    // Note: No Update or Delete methods - timeline entries are append-only

    private static TimelineEntryResponse MapToResponse(IncidentTimelineEntry entry)
    {
        return new TimelineEntryResponse(
            entry.Id,
            entry.IncidentId,
            entry.EntryType,
            entry.OccurredAt,
            entry.BodyMarkdown,
            entry.CreatedAt
        );
    }
}
