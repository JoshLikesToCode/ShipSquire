using ShipSquire.Application.DTOs;
using ShipSquire.Application.Services;
using ShipSquire.Domain.Interfaces;

namespace ShipSquire.Api.Endpoints;

public static class RunbookSectionEndpoints
{
    public static void MapRunbookSectionEndpoints(this IEndpointRouteBuilder app)
    {
        // Get sections for a runbook
        app.MapGet("/api/runbooks/{runbookId:guid}/sections", async (Guid runbookId, IRunbookRepository repo) =>
        {
            var runbook = await repo.GetByIdWithDetailsAsync(runbookId);
            if (runbook == null) return Results.NotFound();

            return Results.Ok(runbook.Sections.OrderBy(s => s.Order).Select(s => new SectionResponse(
                s.Id, s.Key, s.Title, s.Order, s.BodyMarkdown
            )));
        })
        .WithName("GetSections")
        .WithTags("RunbookSections")
        .Produces<IEnumerable<SectionResponse>>(200)
        .Produces(404);

        // Create section
        app.MapPost("/api/runbooks/{runbookId:guid}/sections", async (Guid runbookId, SectionRequest request, RunbookSectionService service) =>
        {
            var result = await service.CreateAsync(runbookId, request);
            return result == null ? Results.NotFound() : Results.Created($"/api/runbooks/{runbookId}/sections/{result.Id}", result);
        })
        .WithName("CreateSection")
        .WithTags("RunbookSections")
        .Produces<SectionResponse>(201)
        .Produces(404);

        // Update section
        app.MapPatch("/api/runbooks/{runbookId:guid}/sections/{sectionId:guid}", async (Guid runbookId, Guid sectionId, SectionRequest request, RunbookSectionService service) =>
        {
            var result = await service.UpdateAsync(runbookId, sectionId, request);
            return result == null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("UpdateSection")
        .WithTags("RunbookSections")
        .Produces<SectionResponse>(200)
        .Produces(404);

        // Reorder sections
        app.MapPost("/api/runbooks/{runbookId:guid}/sections/reorder", async (Guid runbookId, ReorderSectionsRequest request, RunbookSectionService service) =>
        {
            var result = await service.ReorderAsync(runbookId, request);
            return result ? Results.NoContent() : Results.NotFound();
        })
        .WithName("ReorderSections")
        .WithTags("RunbookSections")
        .Produces(204)
        .Produces(404);

        // Delete section
        app.MapDelete("/api/runbooks/{runbookId:guid}/sections/{sectionId:guid}", async (Guid runbookId, Guid sectionId, RunbookSectionService service) =>
        {
            var deleted = await service.DeleteAsync(runbookId, sectionId);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteSection")
        .WithTags("RunbookSections")
        .Produces(204)
        .Produces(404);
    }
}
