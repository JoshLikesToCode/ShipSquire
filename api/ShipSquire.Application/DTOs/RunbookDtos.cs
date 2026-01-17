namespace ShipSquire.Application.DTOs;

public record RunbookRequest(
    string Title,
    string? Summary
);

public record RunbookResponse(
    Guid Id,
    Guid ServiceId,
    string Title,
    string Status,
    int Version,
    string? Summary,
    string Origin,
    RepoAnalysisResult? Analysis,
    List<SectionResponse> Sections,
    List<VariableResponse> Variables,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public record SectionRequest(
    string Key,
    string Title,
    int Order,
    string BodyMarkdown
);

public record SectionResponse(
    Guid Id,
    string Key,
    string Title,
    int Order,
    string BodyMarkdown
);

public record VariableRequest(
    string Name,
    string? ValueHint,
    bool IsSecret,
    string? Description
);

public record VariableResponse(
    Guid Id,
    string Name,
    string? ValueHint,
    bool IsSecret,
    string? Description
);

public record ReorderSectionsRequest(
    List<SectionOrderItem> Sections
);

public record SectionOrderItem(
    Guid Id,
    int Order
);
