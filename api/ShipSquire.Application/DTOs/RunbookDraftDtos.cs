namespace ShipSquire.Application.DTOs;

public record RunbookSectionDraft(
    string Key,
    string Title,
    int Order,
    string BodyMarkdown
);

public record RunbookDraftRequest(
    Guid ServiceId
);

public record RunbookDraftResult(
    List<RunbookSectionDraft> Sections,
    RepoAnalysisResult Analysis
);
