using System.Text;
using ShipSquire.Application.DTOs;
using ShipSquire.Application.Interfaces;
using ShipSquire.Domain.Entities;
using ShipSquire.Domain.Enums;
using ShipSquire.Domain.Interfaces;

namespace ShipSquire.Application.Services;

public class MarkdownExportService
{
    private readonly IIncidentRepository _incidentRepository;
    private readonly ITimelineEntryRepository _timelineRepository;
    private readonly IPostmortemRepository _postmortemRepository;
    private readonly IServiceRepository _serviceRepository;
    private readonly ICurrentUser _currentUser;

    public MarkdownExportService(
        IIncidentRepository incidentRepository,
        ITimelineEntryRepository timelineRepository,
        IPostmortemRepository postmortemRepository,
        IServiceRepository serviceRepository,
        ICurrentUser currentUser)
    {
        _incidentRepository = incidentRepository;
        _timelineRepository = timelineRepository;
        _postmortemRepository = postmortemRepository;
        _serviceRepository = serviceRepository;
        _currentUser = currentUser;
    }

    public async Task<IncidentExportResponse?> ExportIncidentAsync(Guid incidentId, CancellationToken cancellationToken = default)
    {
        // Get incident with details
        var incident = await _incidentRepository.GetByIdWithDetailsAsync(incidentId, cancellationToken);
        if (incident == null || incident.UserId != _currentUser.UserId)
            return null;

        // Get timeline entries
        var timeline = await _timelineRepository.GetByIncidentIdAsync(incidentId, cancellationToken);

        // Get postmortem if exists
        var postmortem = await _postmortemRepository.GetByIncidentIdAsync(incidentId, cancellationToken);

        // Get service name
        var service = await _serviceRepository.GetByIdAndUserIdAsync(incident.ServiceId, _currentUser.UserId, cancellationToken);

        var markdown = GenerateMarkdown(incident, service, timeline, postmortem);
        var filename = GenerateFilename(incident);

        return new IncidentExportResponse(markdown, filename, "text/markdown");
    }

    private static string GenerateMarkdown(
        Incident incident,
        Service? service,
        IEnumerable<IncidentTimelineEntry> timeline,
        Postmortem? postmortem)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine($"# Incident Report: {SanitizeText(incident.Title)}");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();

        // Metadata table
        sb.AppendLine("## Overview");
        sb.AppendLine();
        sb.AppendLine("| Field | Value |");
        sb.AppendLine("|-------|-------|");
        sb.AppendLine($"| **Service** | {SanitizeText(service?.Name ?? "Unknown")} |");
        sb.AppendLine($"| **Severity** | {incident.Severity.ToUpper()} |");
        sb.AppendLine($"| **Status** | {FormatStatus(incident.Status)} |");
        sb.AppendLine($"| **Started** | {incident.StartedAt:yyyy-MM-dd HH:mm:ss} UTC |");
        if (incident.EndedAt.HasValue)
        {
            sb.AppendLine($"| **Ended** | {incident.EndedAt.Value:yyyy-MM-dd HH:mm:ss} UTC |");
            var duration = incident.EndedAt.Value - incident.StartedAt;
            sb.AppendLine($"| **Duration** | {FormatDuration(duration)} |");
        }
        if (incident.Runbook != null)
        {
            sb.AppendLine($"| **Runbook** | {SanitizeText(incident.Runbook.Title)} |");
        }
        sb.AppendLine();

        // Summary
        if (!string.IsNullOrWhiteSpace(incident.SummaryMarkdown))
        {
            sb.AppendLine("## Summary");
            sb.AppendLine();
            sb.AppendLine(SanitizeText(incident.SummaryMarkdown));
            sb.AppendLine();
        }

        // Timeline
        var timelineList = timeline.OrderBy(t => t.OccurredAt).ToList();
        if (timelineList.Any())
        {
            sb.AppendLine("## Timeline");
            sb.AppendLine();
            foreach (var entry in timelineList)
            {
                var icon = GetEntryTypeEmoji(entry.EntryType);
                sb.AppendLine($"### {entry.OccurredAt:yyyy-MM-dd HH:mm:ss} UTC - {icon} {FormatEntryType(entry.EntryType)}");
                sb.AppendLine();
                sb.AppendLine(SanitizeText(entry.BodyMarkdown));
                sb.AppendLine();
            }
        }
        else
        {
            sb.AppendLine("## Timeline");
            sb.AppendLine();
            sb.AppendLine("*No timeline entries recorded.*");
            sb.AppendLine();
        }

        // Postmortem (if resolved and postmortem exists)
        if (postmortem != null)
        {
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("# Postmortem");
            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(postmortem.ImpactMarkdown))
            {
                sb.AppendLine(SanitizeText(postmortem.ImpactMarkdown));
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(postmortem.RootCauseMarkdown))
            {
                sb.AppendLine(SanitizeText(postmortem.RootCauseMarkdown));
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(postmortem.DetectionMarkdown))
            {
                sb.AppendLine(SanitizeText(postmortem.DetectionMarkdown));
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(postmortem.ResolutionMarkdown))
            {
                sb.AppendLine(SanitizeText(postmortem.ResolutionMarkdown));
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(postmortem.ActionItemsMarkdown))
            {
                sb.AppendLine(SanitizeText(postmortem.ActionItemsMarkdown));
                sb.AppendLine();
            }
        }

        // Footer
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine($"*Exported from ShipSquire on {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC*");

        return sb.ToString();
    }

    private static string GenerateFilename(Incident incident)
    {
        var sanitizedTitle = SanitizeFilename(incident.Title);
        var date = incident.StartedAt.ToString("yyyy-MM-dd");
        return $"incident-{date}-{sanitizedTitle}.md";
    }

    private static string SanitizeText(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        // Remove potential secrets/PII patterns
        // Note: This is a basic sanitization - in production, use more robust PII detection
        var sanitized = text;

        // Remove common secret patterns (API keys, tokens, passwords in common formats)
        sanitized = System.Text.RegularExpressions.Regex.Replace(
            sanitized,
            @"(?i)(api[_-]?key|apikey|api_secret|secret[_-]?key|password|passwd|pwd|token|bearer|auth[_-]?token|access[_-]?token)['""]?\s*[:=]\s*['""]?[\w\-\.]+['""]?",
            "$1=[REDACTED]");

        // Remove potential AWS keys
        sanitized = System.Text.RegularExpressions.Regex.Replace(
            sanitized,
            @"AKIA[0-9A-Z]{16}",
            "[REDACTED_AWS_KEY]");

        // Remove potential JWT tokens (basic pattern)
        sanitized = System.Text.RegularExpressions.Regex.Replace(
            sanitized,
            @"eyJ[A-Za-z0-9_-]+\.eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+",
            "[REDACTED_JWT]");

        return sanitized;
    }

    private static string SanitizeFilename(string filename)
    {
        if (string.IsNullOrEmpty(filename)) return "unnamed";

        // Remove invalid filename characters and limit length
        var sanitized = System.Text.RegularExpressions.Regex.Replace(filename, @"[^\w\s\-]", "");
        sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"\s+", "-");
        sanitized = sanitized.ToLowerInvariant();

        // Limit to 50 characters
        if (sanitized.Length > 50)
            sanitized = sanitized[..50];

        return string.IsNullOrEmpty(sanitized) ? "unnamed" : sanitized;
    }

    private static string FormatStatus(string status)
    {
        return status switch
        {
            IncidentStatus.Open => "Open",
            IncidentStatus.Investigating => "Investigating",
            IncidentStatus.Mitigated => "Mitigated",
            IncidentStatus.Resolved => "Resolved",
            _ => status
        };
    }

    private static string FormatEntryType(string entryType)
    {
        return entryType switch
        {
            TimelineEntryType.Note => "Note",
            TimelineEntryType.Action => "Action",
            TimelineEntryType.Decision => "Decision",
            TimelineEntryType.Observation => "Observation",
            _ => entryType
        };
    }

    private static string GetEntryTypeEmoji(string entryType)
    {
        return entryType switch
        {
            TimelineEntryType.Note => "📝",
            TimelineEntryType.Action => "⚡",
            TimelineEntryType.Decision => "🎯",
            TimelineEntryType.Observation => "👁️",
            _ => "•"
        };
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalDays >= 1)
            return $"{(int)duration.TotalDays}d {duration.Hours}h {duration.Minutes}m";
        if (duration.TotalHours >= 1)
            return $"{(int)duration.TotalHours}h {duration.Minutes}m";
        return $"{(int)duration.TotalMinutes}m";
    }
}

public record IncidentExportResponse(
    string Content,
    string Filename,
    string ContentType
);
