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
