using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShipSquire.Infrastructure.Persistence;
using Xunit;

namespace ShipSquire.Tests.Integration.Auth;

public class GitHubOAuthTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public GitHubOAuthTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        // Don't set X-User-Email header - we're testing OAuth
    }

    [Fact]
    public async Task GetAuthMe_WhenNotAuthenticated_ShouldReturn401()
    {
        // Act
        var response = await _client.GetAsync("/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(Skip = "Requires GitHub OAuth configuration")]
    public async Task GitHubLogin_ShouldRedirectToGitHub()
    {
        // Act
        var response = await _client.GetAsync("/auth/github/login");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var location = response.Headers.Location?.ToString();
        location.Should().NotBeNull();
        location.Should().Contain("github.com/login/oauth/authorize");
        location.Should().Contain("client_id=");
    }

    [Fact]
    public async Task GitHubCallback_WithMissingCode_ShouldReturnBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/auth/github/callback");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(Skip = "Requires GitHub OAuth configuration")]
    public async Task GitHubCallback_WithError_ShouldRedirectWithError()
    {
        // Act
        var response = await _client.GetAsync("/auth/github/callback?error=access_denied");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var location = response.Headers.Location?.ToString();
        location.Should().Contain("error=");
    }

    [Fact]
    public async Task UserLinking_CreateNewUserFromGitHub_ShouldSucceed()
    {
        // Arrange - Simulate OAuth flow by creating user directly
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ShipSquireDbContext>();

        var newUser = new Domain.Entities.User
        {
            Email = "testuser@github.local",
            DisplayName = "Test User",
            AuthProvider = "github",
            GitHubUserId = "12345",
            GitHubUsername = "testuser",
            GitHubAccessToken = "encrypted_token_here"
        };

        dbContext.Users.Add(newUser);
        await dbContext.SaveChangesAsync();

        // Act - Find the user by GitHub ID
        var foundUser = await dbContext.Users
            .FirstOrDefaultAsync(u => u.GitHubUserId == "12345");

        // Assert
        foundUser.Should().NotBeNull();
        foundUser!.Email.Should().Be("testuser@github.local");
        foundUser.GitHubUsername.Should().Be("testuser");
        foundUser.AuthProvider.Should().Be("github");
    }

    [Fact]
    public async Task UserLinking_UpdateExistingUserFromGitHub_ShouldSucceed()
    {
        // Arrange - Create initial user
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ShipSquireDbContext>();

        var existingUser = new Domain.Entities.User
        {
            Email = "olduser@github.local",
            DisplayName = "Old Name",
            AuthProvider = "github",
            GitHubUserId = "67890",
            GitHubUsername = "oldusername",
            GitHubAccessToken = "old_token"
        };

        dbContext.Users.Add(existingUser);
        await dbContext.SaveChangesAsync();

        // Act - Update the user (simulating re-login)
        var userToUpdate = await dbContext.Users
            .FirstOrDefaultAsync(u => u.GitHubUserId == "67890");

        userToUpdate!.Email = "newuser@github.local";
        userToUpdate.DisplayName = "New Name";
        userToUpdate.GitHubUsername = "newusername";
        userToUpdate.GitHubAccessToken = "new_token";
        userToUpdate.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync();

        // Assert - Verify updates
        var updatedUser = await dbContext.Users
            .FirstOrDefaultAsync(u => u.GitHubUserId == "67890");

        updatedUser.Should().NotBeNull();
        updatedUser!.Email.Should().Be("newuser@github.local");
        updatedUser.DisplayName.Should().Be("New Name");
        updatedUser.GitHubUsername.Should().Be("newusername");
        updatedUser.GitHubAccessToken.Should().Be("new_token");
    }

    [Fact(Skip = "Requires authentication middleware configuration")]
    public async Task Logout_ShouldReturnOk()
    {
        // Act
        var response = await _client.PostAsync("/auth/logout", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<dynamic>();
        result.Should().NotBeNull();
    }
}
