using System.Net.Http.Headers;
using System.Net.Http.Json;
using ShipSquire.Application.DTOs;
using ShipSquire.Application.Services;
using ShipSquire.Domain.Interfaces;

namespace ShipSquire.Infrastructure.Services;

public interface IGitHubApiClient
{
    Task<List<GitHubRepositoryResponse>> GetUserRepositoriesAsync(string accessToken, int page = 1, int perPage = 30, CancellationToken cancellationToken = default);
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
}
