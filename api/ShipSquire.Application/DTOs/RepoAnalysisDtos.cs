namespace ShipSquire.Application.DTOs;

public record RepoAnalysisResult(
    bool HasDockerfile,
    bool HasCompose,
    bool HasKubernetes,
    bool HasGithubActions,
    List<int> DetectedPorts,
    string AppType, // "aspnet", "node", "mixed", "unknown"
    bool HasReadme,
    bool HasLaunchSettings,
    bool HasCsproj,
    string? PrimaryLanguage = null,
    List<string>? TechnologyStack = null
);

public record RepoAnalysisRequest(
    string Owner,
    string Repo,
    string? Branch = null
);
