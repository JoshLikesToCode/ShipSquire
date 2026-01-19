using ShipSquire.Application.DTOs;
using ShipSquire.Application.Interfaces;
using ShipSquire.Domain.Entities;
using ShipSquire.Domain.Enums;
using ShipSquire.Domain.Interfaces;

namespace ShipSquire.Application.Services;

public class IncidentService
{
    private readonly IIncidentRepository _incidentRepository;
    private readonly IServiceRepository _serviceRepository;
    private readonly IRunbookRepository _runbookRepository;
    private readonly ICurrentUser _currentUser;

    public IncidentService(
        IIncidentRepository incidentRepository,
        IServiceRepository serviceRepository,
        IRunbookRepository runbookRepository,
        ICurrentUser currentUser)
    {
        _incidentRepository = incidentRepository;
        _serviceRepository = serviceRepository;
        _runbookRepository = runbookRepository;
        _currentUser = currentUser;
    }

    public async Task<IEnumerable<IncidentResponse>> GetByServiceIdAsync(Guid serviceId, CancellationToken cancellationToken = default)
    {
        // Verify service ownership
        var service = await _serviceRepository.GetByIdAndUserIdAsync(serviceId, _currentUser.UserId, cancellationToken);
        if (service == null) return Enumerable.Empty<IncidentResponse>();

        var incidents = await _incidentRepository.GetByServiceIdAsync(serviceId, cancellationToken);
        return incidents.Select(MapToResponse);
    }

    public async Task<IncidentResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var incident = await _incidentRepository.GetByIdWithDetailsAsync(id, cancellationToken);
        if (incident == null || incident.UserId != _currentUser.UserId) return null;

        return MapToResponse(incident);
    }

    public async Task<IncidentResponse?> CreateAsync(Guid serviceId, IncidentRequest request, CancellationToken cancellationToken = default)
    {
        // Verify service ownership
        var service = await _serviceRepository.GetByIdAndUserIdAsync(serviceId, _currentUser.UserId, cancellationToken);
        if (service == null) return null;

        // Validate severity
        if (!IncidentSeverity.IsValid(request.Severity))
        {
            throw new ArgumentException($"Invalid severity. Must be one of: {string.Join(", ", IncidentSeverity.All)}");
        }

        // Auto-attach the latest runbook (published preferred, draft fallback)
        var latestRunbook = await _runbookRepository.GetLatestForServiceAsync(serviceId, cancellationToken);

        var incident = new Incident
        {
            UserId = _currentUser.UserId,
            ServiceId = serviceId,
            RunbookId = latestRunbook?.Id,
            Title = request.Title,
            Severity = request.Severity,
            Status = IncidentStatus.Open,
            StartedAt = request.StartedAt,
            SummaryMarkdown = request.SummaryMarkdown
        };

        await _incidentRepository.AddAsync(incident, cancellationToken);

        // Reload with details to get runbook title
        var created = await _incidentRepository.GetByIdWithDetailsAsync(incident.Id, cancellationToken);
        return created == null ? null : MapToResponse(created);
    }

    public async Task<IncidentResponse?> UpdateAsync(Guid id, IncidentUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var incident = await _incidentRepository.GetByIdAndUserIdAsync(id, _currentUser.UserId, cancellationToken);
        if (incident == null) return null;

        if (request.Title != null)
            incident.Title = request.Title;

        if (request.Severity != null)
        {
            if (!IncidentSeverity.IsValid(request.Severity))
                throw new ArgumentException($"Invalid severity. Must be one of: {string.Join(", ", IncidentSeverity.All)}");
            incident.Severity = request.Severity;
        }

        if (request.Status != null)
        {
            if (!IncidentStatus.IsValid(request.Status))
                throw new ArgumentException($"Invalid status. Must be one of: {string.Join(", ", IncidentStatus.All)}");
            incident.Status = request.Status;
        }

        if (request.EndedAt != null)
            incident.EndedAt = request.EndedAt;

        if (request.SummaryMarkdown != null)
            incident.SummaryMarkdown = request.SummaryMarkdown;

        incident.UpdatedAt = DateTimeOffset.UtcNow;
        await _incidentRepository.UpdateAsync(incident, cancellationToken);

        var updated = await _incidentRepository.GetByIdWithDetailsAsync(id, cancellationToken);
        return updated == null ? null : MapToResponse(updated);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var incident = await _incidentRepository.GetByIdAndUserIdAsync(id, _currentUser.UserId, cancellationToken);
        if (incident == null) return false;

        await _incidentRepository.DeleteAsync(incident, cancellationToken);
        return true;
    }

    private static IncidentResponse MapToResponse(Incident incident)
    {
        return new IncidentResponse(
            incident.Id,
            incident.ServiceId,
            incident.RunbookId,
            incident.Runbook?.Title,
            incident.Title,
            incident.Severity,
            incident.Status,
            incident.StartedAt,
            incident.EndedAt,
            incident.SummaryMarkdown,
            incident.CreatedAt,
            incident.UpdatedAt
        );
    }
}
