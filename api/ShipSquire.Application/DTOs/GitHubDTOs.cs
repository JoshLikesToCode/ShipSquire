namespace ShipSquire.Application.DTOs;

public record GitHubAccessTokenResponse(
    string access_token,
    string scope,
    string token_type
);

public record GitHubUserResponse(
    long id,
    string login,
    string? email,
    string? name,
    string? avatar_url
);

public record GitHubRepositoryResponse(
    long id,
    string name,
    string full_name,
    string html_url,
    string? description,
    bool @private,
    GitHubRepositoryOwner owner,
    string? language,
    string default_branch,
    DateTimeOffset updated_at
);

public record GitHubRepositoryOwner(
    string login
);
