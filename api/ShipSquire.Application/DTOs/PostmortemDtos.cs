namespace ShipSquire.Application.DTOs;

public record PostmortemResponse(
    Guid Id,
    Guid IncidentId,
    string? ImpactMarkdown,
    string? RootCauseMarkdown,
    string? DetectionMarkdown,
    string? ResolutionMarkdown,
    string? ActionItemsMarkdown,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public record PostmortemUpdateRequest(
    string? ImpactMarkdown = null,
    string? RootCauseMarkdown = null,
    string? DetectionMarkdown = null,
    string? ResolutionMarkdown = null,
    string? ActionItemsMarkdown = null
);
