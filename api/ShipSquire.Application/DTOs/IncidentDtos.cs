namespace ShipSquire.Application.DTOs;

public record IncidentRequest(
    string Title,
    string Severity,
    Guid? RunbookId
);

public record IncidentResponse(
    Guid Id,
    Guid ServiceId,
    Guid? RunbookId,
    string Title,
    string Severity,
    string Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    string? SummaryMarkdown,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);
