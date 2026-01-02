namespace ShipSquire.Application.DTOs;

public record ServiceRequest(
    string Name,
    string Slug,
    string? Description,
    ServiceRepoInfo? Repo
);

public record ServiceRepoInfo(
    string? Provider,
    string? Owner,
    string? Name,
    string? Url
);

public record ServiceResponse(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    ServiceRepoInfo? Repo,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);
