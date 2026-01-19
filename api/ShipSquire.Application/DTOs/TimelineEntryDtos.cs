namespace ShipSquire.Application.DTOs;

public record TimelineEntryRequest(
    string EntryType,
    string BodyMarkdown
);

public record TimelineEntryResponse(
    Guid Id,
    Guid IncidentId,
    string EntryType,
    DateTimeOffset OccurredAt,
    string BodyMarkdown,
    DateTimeOffset CreatedAt
);
