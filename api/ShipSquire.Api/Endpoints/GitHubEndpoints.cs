using Microsoft.EntityFrameworkCore;
using ShipSquire.Application.DTOs;
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

        // Analyze a GitHub repository
        app.MapPost("/api/github/analyze", async (
            RepoAnalysisRequest request,
            ICurrentUser currentUser,
            IUserRepository userRepository,
            IRepoAnalyzer analyzer,
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
                var result = await analyzer.AnalyzeRepositoryAsync(
                    user.GitHubAccessToken,
                    request.Owner,
                    request.Repo,
                    request.Branch,
                    cancellationToken
                );

                return Results.Ok(result);
            }
            catch (HttpRequestException ex)
            {
                return Results.Problem(
                    title: "Failed to analyze repository from GitHub",
                    detail: ex.Message,
                    statusCode: 502
                );
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Failed to analyze repository",
                    detail: ex.Message,
                    statusCode: 500
                );
            }
        })
        .WithName("AnalyzeGitHubRepository")
        .WithTags("GitHub")
        .Produces<RepoAnalysisResult>(200)
        .Produces(400)
        .Produces(404)
        .Produces(500)
        .Produces(502);
    }
}
