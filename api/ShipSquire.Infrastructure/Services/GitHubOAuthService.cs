using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using ShipSquire.Application.DTOs;
using ShipSquire.Application.Services;
using ShipSquire.Domain.Entities;
using ShipSquire.Domain.Interfaces;

namespace ShipSquire.Infrastructure.Services;

public interface IGitHubOAuthService
{
    Task<string> ExchangeCodeForTokenAsync(string code, CancellationToken cancellationToken = default);
    Task<GitHubUserResponse> GetGitHubUserAsync(string accessToken, CancellationToken cancellationToken = default);
    Task<User> CreateOrUpdateUserFromGitHubAsync(GitHubUserResponse githubUser, string accessToken, CancellationToken cancellationToken = default);
}

public class GitHubOAuthService : IGitHubOAuthService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IUserRepository _userRepository;
    private readonly ITokenEncryptionService _encryptionService;
    private readonly string _clientId;
    private readonly string _clientSecret;

    public GitHubOAuthService(
        IHttpClientFactory httpClientFactory,
        IUserRepository userRepository,
        ITokenEncryptionService encryptionService,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _userRepository = userRepository;
        _encryptionService = encryptionService;
        _clientId = configuration["GitHub:ClientId"] ?? throw new InvalidOperationException("GitHub:ClientId not configured");
        _clientSecret = configuration["GitHub:ClientSecret"] ?? throw new InvalidOperationException("GitHub:ClientSecret not configured");
    }

    public async Task<string> ExchangeCodeForTokenAsync(string code, CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var requestBody = new
        {
            client_id = _clientId,
            client_secret = _clientSecret,
            code = code
        };

        var response = await client.PostAsJsonAsync(
            "https://github.com/login/oauth/access_token",
            requestBody,
            cancellationToken
        );

        response.EnsureSuccessStatusCode();

        var tokenResponse = await response.Content.ReadFromJsonAsync<GitHubAccessTokenResponse>(cancellationToken: cancellationToken);

        if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.access_token))
        {
            throw new InvalidOperationException("Failed to retrieve access token from GitHub");
        }

        return tokenResponse.access_token;
    }

    public async Task<GitHubUserResponse> GetGitHubUserAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ShipSquire", "1.0"));

        var response = await client.GetAsync("https://api.github.com/user", cancellationToken);
        response.EnsureSuccessStatusCode();

        var user = await response.Content.ReadFromJsonAsync<GitHubUserResponse>(cancellationToken: cancellationToken);

        if (user == null)
        {
            throw new InvalidOperationException("Failed to retrieve user information from GitHub");
        }

        return user;
    }

    public async Task<User> CreateOrUpdateUserFromGitHubAsync(
        GitHubUserResponse githubUser,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        // Try to find existing user by GitHub ID
        var user = await _userRepository.GetByGitHubUserIdAsync(githubUser.id.ToString(), cancellationToken);

        var encryptedToken = _encryptionService.Encrypt(accessToken);

        if (user == null)
        {
            // Create new user
            user = new User
            {
                Email = githubUser.email ?? $"{githubUser.login}@github.local",
                DisplayName = githubUser.name ?? githubUser.login,
                AuthProvider = "github",
                GitHubUserId = githubUser.id.ToString(),
                GitHubUsername = githubUser.login,
                GitHubAccessToken = encryptedToken
            };

            await _userRepository.AddAsync(user, cancellationToken);
        }
        else
        {
            // Update existing user
            user.Email = githubUser.email ?? user.Email;
            user.DisplayName = githubUser.name ?? user.DisplayName;
            user.GitHubUsername = githubUser.login;
            user.GitHubAccessToken = encryptedToken;
            user.AuthProvider = "github";
            user.UpdatedAt = DateTimeOffset.UtcNow;

            await _userRepository.UpdateAsync(user, cancellationToken);
        }

        return user;
    }
}
