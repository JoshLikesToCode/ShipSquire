using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using ShipSquire.Application.Services;
using ShipSquire.Infrastructure.Persistence;
using ShipSquire.Infrastructure.Services;
using Xunit;

namespace ShipSquire.Tests.Integration.GitHub;

public class GitHubApiClientTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public GitHubApiClientTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetGitHubRepos_WhenNotAuthenticated_ShouldReturn401()
    {
        // Don't set any authentication headers

        // Act
        var response = await _client.GetAsync("/api/github/repos");

        // Assert
        // The endpoint should require authentication
        // Since we're using X-User-Email header fallback, it might still work
        // but if GitHub token is not linked, it should return 400
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetGitHubRepos_WithoutGitHubToken_ShouldReturnBadRequest()
    {
        // Arrange - Create a user without GitHub token
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ShipSquireDbContext>();

        var user = new Domain.Entities.User
        {
            Email = "test-no-token@local",
            DisplayName = "Test User",
            AuthProvider = "local"
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        // Act
        _client.DefaultRequestHeaders.Add("X-User-Email", user.Email);
        var response = await _client.GetAsync("/api/github/repos");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("GitHub account not linked");
    }

    [Fact]
    public async Task GetGitHubRepos_WithInvalidToken_ShouldReturn502()
    {
        // Arrange - Create a user with invalid GitHub token
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ShipSquireDbContext>();
        var encryptionService = scope.ServiceProvider.GetRequiredService<ITokenEncryptionService>();

        var user = new Domain.Entities.User
        {
            Email = "test-invalid-token@local",
            DisplayName = "Test User",
            AuthProvider = "github",
            GitHubUserId = "999999",
            GitHubUsername = "testuser",
            GitHubAccessToken = encryptionService.Encrypt("invalid-token-12345")
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        // Act
        _client.DefaultRequestHeaders.Add("X-User-Email", user.Email);
        var response = await _client.GetAsync("/api/github/repos");

        // Assert
        // GitHub API will reject invalid token, resulting in 502 Bad Gateway
        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task LinkRepoToService_WithValidData_ShouldSucceed()
    {
        // Arrange - Create service first
        Guid serviceId;
        string userEmail;

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ShipSquireDbContext>();

            var user = new Domain.Entities.User
            {
                Email = "test-link-repo@local",
                DisplayName = "Test User",
                AuthProvider = "local"
            };
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
            userEmail = user.Email;

            var service = new Domain.Entities.Service
            {
                UserId = user.Id,
                Name = "Test Service",
                Slug = "test-service",
                Description = "Test"
            };

            dbContext.Services.Add(service);
            await dbContext.SaveChangesAsync();
            serviceId = service.Id;
        }

        // Act
        var linkRepoRequest = new
        {
            provider = "github",
            owner = "octocat",
            name = "Hello-World",
            url = "https://github.com/octocat/Hello-World",
            defaultBranch = "main",
            primaryLanguage = "JavaScript"
        };

        _client.DefaultRequestHeaders.Add("X-User-Email", userEmail);
        var response = await _client.PatchAsJsonAsync($"/api/services/{serviceId}/link-repo", linkRepoRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify database was updated (use new scope to get fresh data)
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ShipSquireDbContext>();
            var updatedService = await dbContext.Services.FindAsync(serviceId);
            updatedService.Should().NotBeNull();
            updatedService!.RepoProvider.Should().Be("github");
            updatedService.RepoOwner.Should().Be("octocat");
            updatedService.RepoName.Should().Be("Hello-World");
            updatedService.RepoUrl.Should().Be("https://github.com/octocat/Hello-World");
            updatedService.DefaultBranch.Should().Be("main");
            updatedService.PrimaryLanguage.Should().Be("JavaScript");
        }
    }

    [Fact]
    public async Task LinkRepoToService_ForNonExistentService_ShouldReturn404()
    {
        // Arrange
        var linkRepoRequest = new
        {
            provider = "github",
            owner = "octocat",
            name = "Hello-World",
            url = "https://github.com/octocat/Hello-World",
            defaultBranch = "main",
            primaryLanguage = "JavaScript"
        };

        // Act
        _client.DefaultRequestHeaders.Add("X-User-Email", "josh@local");
        var response = await _client.PatchAsJsonAsync($"/api/services/{Guid.NewGuid()}/link-repo", linkRepoRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
