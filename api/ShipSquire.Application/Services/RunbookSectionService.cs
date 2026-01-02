using ShipSquire.Application.DTOs;
using ShipSquire.Application.Interfaces;
using ShipSquire.Domain.Entities;
using ShipSquire.Domain.Interfaces;

namespace ShipSquire.Application.Services;

public class RunbookSectionService
{
    private readonly IRunbookRepository _runbookRepository;
    private readonly IRepository<RunbookSection> _sectionRepository;
    private readonly ICurrentUser _currentUser;

    public RunbookSectionService(
        IRunbookRepository runbookRepository,
        IRepository<RunbookSection> sectionRepository,
        ICurrentUser currentUser)
    {
        _runbookRepository = runbookRepository;
        _sectionRepository = sectionRepository;
        _currentUser = currentUser;
    }

    public async Task<SectionResponse?> CreateAsync(Guid runbookId, SectionRequest request, CancellationToken cancellationToken = default)
    {
        var runbook = await _runbookRepository.GetByIdAndUserIdAsync(runbookId, _currentUser.UserId, cancellationToken);
        if (runbook == null) return null;

        var section = new RunbookSection
        {
            RunbookId = runbookId,
            Key = request.Key,
            Title = request.Title,
            Order = request.Order,
            BodyMarkdown = request.BodyMarkdown
        };

        await _sectionRepository.AddAsync(section, cancellationToken);
        return MapToResponse(section);
    }

    public async Task<SectionResponse?> UpdateAsync(Guid runbookId, Guid sectionId, SectionRequest request, CancellationToken cancellationToken = default)
    {
        var runbook = await _runbookRepository.GetByIdAndUserIdAsync(runbookId, _currentUser.UserId, cancellationToken);
        if (runbook == null) return null;

        var section = await _sectionRepository.GetByIdAsync(sectionId, cancellationToken);
        if (section == null || section.RunbookId != runbookId) return null;

        section.Key = request.Key;
        section.Title = request.Title;
        section.Order = request.Order;
        section.BodyMarkdown = request.BodyMarkdown;
        section.UpdatedAt = DateTimeOffset.UtcNow;

        await _sectionRepository.UpdateAsync(section, cancellationToken);
        return MapToResponse(section);
    }

    public async Task<bool> ReorderAsync(Guid runbookId, ReorderSectionsRequest request, CancellationToken cancellationToken = default)
    {
        var runbook = await _runbookRepository.GetByIdWithDetailsAsync(runbookId, cancellationToken);
        if (runbook == null || runbook.UserId != _currentUser.UserId) return false;

        // Update all sections in memory first
        foreach (var item in request.Sections)
        {
            var section = runbook.Sections.FirstOrDefault(s => s.Id == item.Id);
            if (section != null)
            {
                section.Order = item.Order;
                section.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }

        // Batch update all sections at once (avoid N+1)
        foreach (var section in runbook.Sections.Where(s => request.Sections.Any(rs => rs.Id == s.Id)))
        {
            await _sectionRepository.UpdateAsync(section, cancellationToken);
        }

        return true;
    }

    public async Task<bool> DeleteAsync(Guid runbookId, Guid sectionId, CancellationToken cancellationToken = default)
    {
        var runbook = await _runbookRepository.GetByIdAndUserIdAsync(runbookId, _currentUser.UserId, cancellationToken);
        if (runbook == null) return false;

        var section = await _sectionRepository.GetByIdAsync(sectionId, cancellationToken);
        if (section == null || section.RunbookId != runbookId) return false;

        await _sectionRepository.DeleteAsync(section, cancellationToken);
        return true;
    }

    private static SectionResponse MapToResponse(RunbookSection section)
    {
        return new SectionResponse(
            section.Id,
            section.Key,
            section.Title,
            section.Order,
            section.BodyMarkdown
        );
    }
}
