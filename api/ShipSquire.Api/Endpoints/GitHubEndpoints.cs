using Microsoft.EntityFrameworkCore;
using ShipSquire.Application.Interfaces;
using ShipSquire.Domain.Interfaces;
using ShipSquire.Infrastructure.Services;

namespace ShipSquire.Api.Endpoints;

public static class GitHubEndpoints
{
    public static void MapGitHubEndpoints(this IEndpointRouteBuilder app)
    {
        // Get user's GitHub repositories
        app.MapGet("/api/github/repos", async (
            ICurrentUser currentUser,
            IUserRepository userRepository,
            IGitHubApiClient githubClient,
            int page = 1,
            int perPage = 30,
            CancellationToken cancellationToken = default) =>
        {
            var userId = currentUser.UserId;
            var user = await userRepository.GetByIdAsync(userId, cancellationToken);

            if (user == null)
            {
                return Results.NotFound(new { error = "User not found" });
            }

            if (string.IsNullOrEmpty(user.GitHubAccessToken))
            {
                return Results.BadRequest(new { error = "GitHub account not linked. Please log in with GitHub." });
            }

            try
            {
                var repositories = await githubClient.GetUserRepositoriesAsync(
                    user.GitHubAccessToken,
                    page,
                    perPage,
                    cancellationToken
                );

                return Results.Ok(repositories);
            }
            catch (HttpRequestException ex)
            {
                return Results.Problem(
                    title: "Failed to fetch repositories from GitHub",
                    detail: ex.Message,
                    statusCode: 502
                );
            }
        })
        .WithName("GetUserGitHubRepositories")
        .WithTags("GitHub")
        .Produces(200)
        .Produces(400)
        .Produces(404)
        .Produces(502);
    }
}
