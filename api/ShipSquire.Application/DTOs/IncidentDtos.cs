namespace ShipSquire.Application.DTOs;

public record IncidentRequest(
    string Title,
    string Severity,
    DateTimeOffset StartedAt,
    string? SummaryMarkdown = null
);

public record IncidentResponse(
    Guid Id,
    Guid ServiceId,
    Guid? RunbookId,
    string? RunbookTitle,
    string Title,
    string Severity,
    string Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    string? SummaryMarkdown,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public record IncidentUpdateRequest(
    string? Title = null,
    string? Severity = null,
    string? Status = null,
    DateTimeOffset? EndedAt = null,
    string? SummaryMarkdown = null
);
