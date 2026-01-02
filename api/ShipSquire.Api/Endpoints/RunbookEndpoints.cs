using ShipSquire.Application.DTOs;
using ShipSquire.Application.Services;

namespace ShipSquire.Api.Endpoints;

public static class RunbookEndpoints
{
    public static void MapRunbookEndpoints(this IEndpointRouteBuilder app)
    {
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
