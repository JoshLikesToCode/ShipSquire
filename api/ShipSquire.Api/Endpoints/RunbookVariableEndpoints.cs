using ShipSquire.Application.DTOs;
using ShipSquire.Application.Services;
using ShipSquire.Domain.Interfaces;

namespace ShipSquire.Api.Endpoints;

public static class RunbookVariableEndpoints
{
    public static void MapRunbookVariableEndpoints(this IEndpointRouteBuilder app)
    {
        // Get variables for a runbook
        app.MapGet("/api/runbooks/{runbookId:guid}/variables", async (Guid runbookId, IRunbookRepository repo) =>
        {
            var runbook = await repo.GetByIdWithDetailsAsync(runbookId);
            if (runbook == null) return Results.NotFound();

            return Results.Ok(runbook.Variables.Select(v => new VariableResponse(
                v.Id, v.Name, v.ValueHint, v.IsSecret, v.Description
            )));
        })
        .WithName("GetVariables")
        .WithTags("RunbookVariables")
        .Produces<IEnumerable<VariableResponse>>(200)
        .Produces(404);

        // Create variable
        app.MapPost("/api/runbooks/{runbookId:guid}/variables", async (Guid runbookId, VariableRequest request, RunbookVariableService service) =>
        {
            var result = await service.CreateAsync(runbookId, request);
            return result == null ? Results.NotFound() : Results.Created($"/api/runbooks/{runbookId}/variables/{result.Id}", result);
        })
        .WithName("CreateVariable")
        .WithTags("RunbookVariables")
        .Produces<VariableResponse>(201)
        .Produces(404);

        // Update variable
        app.MapPatch("/api/runbooks/{runbookId:guid}/variables/{variableId:guid}", async (Guid runbookId, Guid variableId, VariableRequest request, RunbookVariableService service) =>
        {
            var result = await service.UpdateAsync(runbookId, variableId, request);
            return result == null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("UpdateVariable")
        .WithTags("RunbookVariables")
        .Produces<VariableResponse>(200)
        .Produces(404);

        // Delete variable
        app.MapDelete("/api/runbooks/{runbookId:guid}/variables/{variableId:guid}", async (Guid runbookId, Guid variableId, RunbookVariableService service) =>
        {
            var deleted = await service.DeleteAsync(runbookId, variableId);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteVariable")
        .WithTags("RunbookVariables")
        .Produces(204)
        .Produces(404);
    }
}
