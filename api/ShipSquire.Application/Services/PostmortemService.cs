using System.Text;
using ShipSquire.Application.DTOs;
using ShipSquire.Application.Interfaces;
using ShipSquire.Domain.Entities;
using ShipSquire.Domain.Enums;
using ShipSquire.Domain.Interfaces;

namespace ShipSquire.Application.Services;

public class PostmortemService
{
    private readonly IPostmortemRepository _postmortemRepository;
    private readonly IIncidentRepository _incidentRepository;
    private readonly ITimelineEntryRepository _timelineRepository;
    private readonly ICurrentUser _currentUser;

    public PostmortemService(
        IPostmortemRepository postmortemRepository,
        IIncidentRepository incidentRepository,
        ITimelineEntryRepository timelineRepository,
        ICurrentUser currentUser)
    {
        _postmortemRepository = postmortemRepository;
        _incidentRepository = incidentRepository;
        _timelineRepository = timelineRepository;
        _currentUser = currentUser;
    }

    public async Task<PostmortemResponse?> GetByIncidentIdAsync(Guid incidentId, CancellationToken cancellationToken = default)
    {
        // Verify incident ownership
        var incident = await _incidentRepository.GetByIdWithDetailsAsync(incidentId, cancellationToken);
        if (incident == null || incident.UserId != _currentUser.UserId) return null;

        var postmortem = await _postmortemRepository.GetByIncidentIdAsync(incidentId, cancellationToken);

        // Auto-generate if doesn't exist and incident is resolved
        if (postmortem == null)
        {
            if (incident.Status != IncidentStatus.Resolved)
            {
                return null; // No postmortem for non-resolved incidents
            }

            postmortem = await GenerateAndSavePostmortemAsync(incident, cancellationToken);
        }

        return MapToResponse(postmortem);
    }

    public async Task<PostmortemResponse?> UpdateAsync(Guid incidentId, PostmortemUpdateRequest request, CancellationToken cancellationToken = default)
    {
        // Verify incident ownership
        var incident = await _incidentRepository.GetByIdAndUserIdAsync(incidentId, _currentUser.UserId, cancellationToken);
        if (incident == null) return null;

        var postmortem = await _postmortemRepository.GetByIncidentIdAsync(incidentId, cancellationToken);

        // Create if doesn't exist
        if (postmortem == null)
        {
            var fullIncident = await _incidentRepository.GetByIdWithDetailsAsync(incidentId, cancellationToken);
            if (fullIncident == null) return null;
            postmortem = await GenerateAndSavePostmortemAsync(fullIncident, cancellationToken);
        }

        // Apply updates
        if (request.ImpactMarkdown != null)
            postmortem.ImpactMarkdown = request.ImpactMarkdown;

        if (request.RootCauseMarkdown != null)
            postmortem.RootCauseMarkdown = request.RootCauseMarkdown;

        if (request.DetectionMarkdown != null)
            postmortem.DetectionMarkdown = request.DetectionMarkdown;

        if (request.ResolutionMarkdown != null)
            postmortem.ResolutionMarkdown = request.ResolutionMarkdown;

        if (request.ActionItemsMarkdown != null)
            postmortem.ActionItemsMarkdown = request.ActionItemsMarkdown;

        postmortem.UpdatedAt = DateTimeOffset.UtcNow;
        await _postmortemRepository.UpdateAsync(postmortem, cancellationToken);

        return MapToResponse(postmortem);
    }

    private async Task<Postmortem> GenerateAndSavePostmortemAsync(Incident incident, CancellationToken cancellationToken)
    {
        var timeline = await _timelineRepository.GetByIncidentIdAsync(incident.Id, cancellationToken);

        var postmortem = new Postmortem
        {
            IncidentId = incident.Id,
            ImpactMarkdown = GenerateImpactSection(incident),
            RootCauseMarkdown = GenerateRootCauseSection(incident, timeline),
            DetectionMarkdown = GenerateDetectionSection(incident, timeline),
            ResolutionMarkdown = GenerateResolutionSection(incident, timeline),
            ActionItemsMarkdown = GenerateActionItemsSection()
        };

        await _postmortemRepository.AddAsync(postmortem, cancellationToken);
        return postmortem;
    }

    private static string GenerateImpactSection(Incident incident)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"## Impact Summary");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(incident.SummaryMarkdown))
        {
            sb.AppendLine(incident.SummaryMarkdown);
            sb.AppendLine();
        }

        sb.AppendLine($"- **Severity**: {incident.Severity.ToUpper()}");
        sb.AppendLine($"- **Started**: {incident.StartedAt:yyyy-MM-dd HH:mm:ss} UTC");

        if (incident.EndedAt.HasValue)
        {
            var duration = incident.EndedAt.Value - incident.StartedAt;
            sb.AppendLine($"- **Ended**: {incident.EndedAt.Value:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine($"- **Duration**: {FormatDuration(duration)}");
        }

        sb.AppendLine();
        sb.AppendLine("*TODO: Describe the customer and business impact.*");

        return sb.ToString();
    }

    private static string GenerateRootCauseSection(Incident incident, IEnumerable<IncidentTimelineEntry> timeline)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Root Cause Analysis");
        sb.AppendLine();

        // Include decisions from timeline as starting points
        var decisions = timeline
            .Where(e => e.EntryType == TimelineEntryType.Decision)
            .ToList();

        if (decisions.Any())
        {
            sb.AppendLine("### Decisions Made During Incident");
            foreach (var decision in decisions)
            {
                sb.AppendLine($"- [{decision.OccurredAt:HH:mm}] {decision.BodyMarkdown}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("*TODO: Identify the root cause using 5 Whys or similar technique.*");

        return sb.ToString();
    }

    private static string GenerateDetectionSection(Incident incident, IEnumerable<IncidentTimelineEntry> timeline)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Detection");
        sb.AppendLine();

        // Include observations from timeline
        var observations = timeline
            .Where(e => e.EntryType == TimelineEntryType.Observation)
            .ToList();

        if (observations.Any())
        {
            sb.AppendLine("### Observations During Incident");
            foreach (var obs in observations)
            {
                sb.AppendLine($"- [{obs.OccurredAt:HH:mm}] {obs.BodyMarkdown}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("*TODO: How was the incident detected? Could detection be improved?*");

        return sb.ToString();
    }

    private static string GenerateResolutionSection(Incident incident, IEnumerable<IncidentTimelineEntry> timeline)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Resolution");
        sb.AppendLine();

        // Reference runbook if attached
        if (incident.Runbook != null)
        {
            sb.AppendLine($"### Runbook Reference");
            sb.AppendLine($"- **Runbook**: {incident.Runbook.Title}");
            sb.AppendLine();
        }

        // Include actions from timeline
        var actions = timeline
            .Where(e => e.EntryType == TimelineEntryType.Action)
            .ToList();

        if (actions.Any())
        {
            sb.AppendLine("### Actions Taken");
            foreach (var action in actions)
            {
                sb.AppendLine($"- [{action.OccurredAt:HH:mm}] {action.BodyMarkdown}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("*TODO: Describe the resolution steps and confirm the fix.*");

        return sb.ToString();
    }

    private static string GenerateActionItemsSection()
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Action Items");
        sb.AppendLine();
        sb.AppendLine("| Action | Owner | Due Date | Status |");
        sb.AppendLine("|--------|-------|----------|--------|");
        sb.AppendLine("| *TODO: Add action items* | | | |");
        sb.AppendLine();
        sb.AppendLine("*TODO: List follow-up actions to prevent recurrence.*");

        return sb.ToString();
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalDays >= 1)
            return $"{(int)duration.TotalDays}d {duration.Hours}h {duration.Minutes}m";
        if (duration.TotalHours >= 1)
            return $"{(int)duration.TotalHours}h {duration.Minutes}m";
        return $"{(int)duration.TotalMinutes}m";
    }

    private static PostmortemResponse MapToResponse(Postmortem postmortem)
    {
        return new PostmortemResponse(
            postmortem.Id,
            postmortem.IncidentId,
            postmortem.ImpactMarkdown,
            postmortem.RootCauseMarkdown,
            postmortem.DetectionMarkdown,
            postmortem.ResolutionMarkdown,
            postmortem.ActionItemsMarkdown,
            postmortem.CreatedAt,
            postmortem.UpdatedAt
        );
    }
}
