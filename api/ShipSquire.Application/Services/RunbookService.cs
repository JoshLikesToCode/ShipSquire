using ShipSquire.Application.DTOs;
using ShipSquire.Application.Interfaces;
using ShipSquire.Domain.Entities;
using ShipSquire.Domain.Interfaces;

namespace ShipSquire.Application.Services;

public class RunbookService
{
    private readonly IRunbookRepository _runbookRepository;
    private readonly IServiceRepository _serviceRepository;
    private readonly ICurrentUser _currentUser;

    private static readonly List<(string Key, string Title, int Order)> DefaultSections = new()
    {
        ("overview", "Overview", 1),
        ("deploy", "Deployment", 2),
        ("rollback", "Rollback", 3),
        ("health_checks", "Health Checks", 4),
        ("env_vars", "Environment Variables", 5),
        ("troubleshooting", "Troubleshooting", 6)
    };

    public RunbookService(
        IRunbookRepository runbookRepository,
        IServiceRepository serviceRepository,
        ICurrentUser currentUser)
    {
        _runbookRepository = runbookRepository;
        _serviceRepository = serviceRepository;
        _currentUser = currentUser;
    }

    public async Task<IEnumerable<RunbookResponse>> GetByServiceIdAsync(Guid serviceId, CancellationToken cancellationToken = default)
    {
        // Verify service ownership
        var service = await _serviceRepository.GetByIdAndUserIdAsync(serviceId, _currentUser.UserId, cancellationToken);
        if (service == null) return Enumerable.Empty<RunbookResponse>();

        var runbooks = await _runbookRepository.GetByServiceIdAsync(serviceId, cancellationToken);
        return runbooks.Select(MapToResponse);
    }

    public async Task<RunbookResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var runbook = await _runbookRepository.GetByIdWithDetailsAsync(id, cancellationToken);
        if (runbook == null || runbook.UserId != _currentUser.UserId) return null;

        return MapToResponse(runbook);
    }

    public async Task<RunbookResponse?> CreateAsync(Guid serviceId, RunbookRequest request, CancellationToken cancellationToken = default)
    {
        // Verify service ownership
        var service = await _serviceRepository.GetByIdAndUserIdAsync(serviceId, _currentUser.UserId, cancellationToken);
        if (service == null) return null;

        var runbook = new Runbook
        {
            UserId = _currentUser.UserId,
            ServiceId = serviceId,
            Title = request.Title,
            Summary = request.Summary,
            Status = "draft",
            Version = 1
        };

        // Add default sections
        foreach (var (key, title, order) in DefaultSections)
        {
            runbook.Sections.Add(new RunbookSection
            {
                RunbookId = runbook.Id,
                Key = key,
                Title = title,
                Order = order,
                BodyMarkdown = $"# {title}\n\nAdd content here..."
            });
        }

        await _runbookRepository.AddAsync(runbook, cancellationToken);

        // Reload with details
        var created = await _runbookRepository.GetByIdWithDetailsAsync(runbook.Id, cancellationToken);
        return created == null ? null : MapToResponse(created);
    }

    public async Task<RunbookResponse?> UpdateAsync(Guid id, RunbookRequest request, CancellationToken cancellationToken = default)
    {
        var runbook = await _runbookRepository.GetByIdAndUserIdAsync(id, _currentUser.UserId, cancellationToken);
        if (runbook == null) return null;

        runbook.Title = request.Title;
        runbook.Summary = request.Summary;
        runbook.UpdatedAt = DateTimeOffset.UtcNow;

        await _runbookRepository.UpdateAsync(runbook, cancellationToken);

        var updated = await _runbookRepository.GetByIdWithDetailsAsync(id, cancellationToken);
        return updated == null ? null : MapToResponse(updated);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var runbook = await _runbookRepository.GetByIdAndUserIdAsync(id, _currentUser.UserId, cancellationToken);
        if (runbook == null) return false;

        await _runbookRepository.DeleteAsync(runbook, cancellationToken);
        return true;
    }

    private static RunbookResponse MapToResponse(Runbook runbook)
    {
        return new RunbookResponse(
            runbook.Id,
            runbook.ServiceId,
            runbook.Title,
            runbook.Status,
            runbook.Version,
            runbook.Summary,
            runbook.Sections.OrderBy(s => s.Order).Select(s => new SectionResponse(
                s.Id,
                s.Key,
                s.Title,
                s.Order,
                s.BodyMarkdown
            )).ToList(),
            runbook.Variables.Select(v => new VariableResponse(
                v.Id,
                v.Name,
                v.ValueHint,
                v.IsSecret,
                v.Description
            )).ToList(),
            runbook.CreatedAt,
            runbook.UpdatedAt
        );
    }
}
