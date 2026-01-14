using Microsoft.EntityFrameworkCore;
using ShipSquire.Application.DTOs;
using ShipSquire.Application.Interfaces;
using ShipSquire.Application.Services;
using ShipSquire.Domain.Interfaces;

namespace ShipSquire.Api.Endpoints;

public static class RunbookEndpoints
{
    public static void MapRunbookEndpoints(this IEndpointRouteBuilder app)
    {
        // Generate draft runbook for a service
        app.MapPost("/api/services/{serviceId:guid}/runbooks/draft", async (
            Guid serviceId,
            ICurrentUser currentUser,
            IServiceRepository serviceRepository,
            IRepoAnalyzer repoAnalyzer,
            IRunbookDraftGenerator draftGenerator,
            CancellationToken cancellationToken = default) =>
        {
            // Get service with User navigation loaded
            var service = await serviceRepository.GetByIdWithUserAsync(serviceId, currentUser.UserId, cancellationToken);

            if (service == null)
            {
                return Results.NotFound(new { message = "Service not found" });
            }

            // Ensure service has GitHub repository linked
            if (string.IsNullOrEmpty(service.RepoOwner) || string.IsNullOrEmpty(service.RepoName))
            {
                return Results.BadRequest(new { message = "Service does not have a GitHub repository linked" });
            }

            // Ensure user has GitHub token
            if (string.IsNullOrEmpty(service.User.GitHubAccessToken))
            {
                return Results.BadRequest(new { message = "GitHub account not linked. Please log in with GitHub." });
            }

            try
            {
                // Analyze repository
                var analysis = await repoAnalyzer.AnalyzeRepositoryAsync(
                    service.User.GitHubAccessToken,
                    service.RepoOwner,
                    service.RepoName,
                    service.DefaultBranch,
                    cancellationToken);

                // Generate draft sections
                var sections = draftGenerator.GenerateDraft(service, analysis);

                return Results.Ok(new RunbookDraftResult(sections, analysis));
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
                    title: "Failed to generate runbook draft",
                    detail: ex.Message,
                    statusCode: 500
                );
            }
        })
        .WithName("GenerateRunbookDraft")
        .WithTags("Runbooks")
        .Produces<RunbookDraftResult>(200)
        .Produces(400)
        .Produces(404)
        .Produces(500)
        .Produces(502);

        // Get runbooks for a service
        app.MapGet("/api/services/{serviceId:guid}/runbooks", async (Guid serviceId, RunbookService service) =>
        {
            var runbooks = await service.GetByServiceIdAsync(serviceId);
            return Results.Ok(runbooks);
        })
        .WithName("GetServiceRunbooks")
        .WithTags("Runbooks")
        .Produces<IEnumerable<RunbookResponse>>(200);

        // Create runbook for a service
        app.MapPost("/api/services/{serviceId:guid}/runbooks", async (Guid serviceId, RunbookRequest request, RunbookService service) =>
        {
            var result = await service.CreateAsync(serviceId, request);
            return result == null ? Results.NotFound() : Results.Created($"/api/runbooks/{result.Id}", result);
        })
        .WithName("CreateRunbook")
        .WithTags("Runbooks")
        .Produces<RunbookResponse>(201)
        .Produces(404);

        // Get runbook by ID
        app.MapGet("/api/runbooks/{runbookId:guid}", async (Guid runbookId, RunbookService service) =>
        {
            var result = await service.GetByIdAsync(runbookId);
            return result == null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("GetRunbook")
        .WithTags("Runbooks")
        .Produces<RunbookResponse>(200)
        .Produces(404);

        // Update runbook
        app.MapPatch("/api/runbooks/{runbookId:guid}", async (Guid runbookId, RunbookRequest request, RunbookService service) =>
        {
            var result = await service.UpdateAsync(runbookId, request);
            return result == null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("UpdateRunbook")
        .WithTags("Runbooks")
        .Produces<RunbookResponse>(200)
        .Produces(404);

        // Delete runbook
        app.MapDelete("/api/runbooks/{runbookId:guid}", async (Guid runbookId, RunbookService service) =>
        {
            var deleted = await service.DeleteAsync(runbookId);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteRunbook")
        .WithTags("Runbooks")
        .Produces(204)
        .Produces(404);
    }
}
