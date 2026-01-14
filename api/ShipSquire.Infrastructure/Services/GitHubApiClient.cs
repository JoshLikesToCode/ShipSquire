using System.Net.Http.Headers;
using System.Net.Http.Json;
using ShipSquire.Application.DTOs;
using ShipSquire.Application.Services;
using ShipSquire.Domain.Interfaces;

namespace ShipSquire.Infrastructure.Services;

public interface IGitHubApiClient
{
    Task<List<GitHubRepositoryResponse>> GetUserRepositoriesAsync(string accessToken, int page = 1, int perPage = 30, CancellationToken cancellationToken = default);
    Task<GitHubTreeResponse> GetRepositoryTreeAsync(string accessToken, string owner, string repo, string? branch = null, CancellationToken cancellationToken = default);
    Task<GitHubFileContentResponse> GetFileContentAsync(string accessToken, string owner, string repo, string path, CancellationToken cancellationToken = default);
}

public class GitHubApiClient : IGitHubApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ITokenEncryptionService _encryptionService;

    public GitHubApiClient(
        IHttpClientFactory httpClientFactory,
        ITokenEncryptionService encryptionService)
    {
        _httpClientFactory = httpClientFactory;
        _encryptionService = encryptionService;
    }

    public async Task<List<GitHubRepositoryResponse>> GetUserRepositoriesAsync(
        string accessToken,
        int page = 1,
        int perPage = 30,
        CancellationToken cancellationToken = default)
    {
        var decryptedToken = _encryptionService.Decrypt(accessToken);

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", decryptedToken);
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ShipSquire", "1.0"));

        var url = $"https://api.github.com/user/repos?page={page}&per_page={perPage}&sort=updated&affiliation=owner,collaborator";
        var response = await client.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var repositories = await response.Content.ReadFromJsonAsync<List<GitHubRepositoryResponse>>(cancellationToken: cancellationToken);

        return repositories ?? new List<GitHubRepositoryResponse>();
    }

    public async Task<GitHubTreeResponse> GetRepositoryTreeAsync(
        string accessToken,
        string owner,
        string repo,
        string? branch = null,
        CancellationToken cancellationToken = default)
    {
        var decryptedToken = _encryptionService.Decrypt(accessToken);

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", decryptedToken);
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ShipSquire", "1.0"));

        // Get default branch if not specified
        if (string.IsNullOrEmpty(branch))
        {
            var repoUrl = $"https://api.github.com/repos/{owner}/{repo}";
            var repoResponse = await client.GetAsync(repoUrl, cancellationToken);
            repoResponse.EnsureSuccessStatusCode();
            var repoData = await repoResponse.Content.ReadFromJsonAsync<GitHubRepositoryResponse>(cancellationToken: cancellationToken);
            branch = repoData?.default_branch ?? "main";
        }

        // Get tree recursively
        var url = $"https://api.github.com/repos/{owner}/{repo}/git/trees/{branch}?recursive=1";
        var response = await client.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var tree = await response.Content.ReadFromJsonAsync<GitHubTreeResponse>(cancellationToken: cancellationToken);

        if (tree == null)
        {
            throw new InvalidOperationException("Failed to retrieve repository tree from GitHub");
        }

        return tree;
    }

    public async Task<GitHubFileContentResponse> GetFileContentAsync(
        string accessToken,
        string owner,
        string repo,
        string path,
        CancellationToken cancellationToken = default)
    {
        var decryptedToken = _encryptionService.Decrypt(accessToken);

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", decryptedToken);
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ShipSquire", "1.0"));

        var url = $"https://api.github.com/repos/{owner}/{repo}/contents/{path}";
        var response = await client.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var fileContent = await response.Content.ReadFromJsonAsync<GitHubFileContentResponse>(cancellationToken: cancellationToken);

        if (fileContent == null)
        {
            throw new InvalidOperationException($"Failed to retrieve file content for {path} from GitHub");
        }

        return fileContent;
    }
}
