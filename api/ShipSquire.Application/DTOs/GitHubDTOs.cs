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

public record GitHubTreeResponse(
    string sha,
    string url,
    List<GitHubTreeItem> tree,
    bool truncated
);

public record GitHubTreeItem(
    string path,
    string mode,
    string type, // "blob" or "tree"
    string sha,
    long? size,
    string? url
);

public record GitHubFileContentResponse(
    string name,
    string path,
    string sha,
    long size,
    string url,
    string? content, // Base64 encoded
    string encoding
);
