using ShipSquire.Application.DTOs;
using ShipSquire.Application.Services;

namespace ShipSquire.Api.Endpoints;

public static class TimelineEndpoints
{
    public static void MapTimelineEndpoints(this IEndpointRouteBuilder app)
    {
        // Get timeline entries for an incident
        app.MapGet("/api/incidents/{incidentId:guid}/timeline", async (Guid incidentId, TimelineEntryService service) =>
        {
            var entries = await service.GetByIncidentIdAsync(incidentId);
            return Results.Ok(entries);
        })
        .WithName("GetIncidentTimeline")
        .WithTags("Timeline")
        .Produces<IEnumerable<TimelineEntryResponse>>(200);

        // Add timeline entry to an incident (append-only)
        app.MapPost("/api/incidents/{incidentId:guid}/timeline", async (Guid incidentId, TimelineEntryRequest request, TimelineEntryService service) =>
        {
            try
            {
                var result = await service.AddEntryAsync(incidentId, request);
                return result == null
                    ? Results.NotFound(new { message = "Incident not found" })
                    : Results.Created($"/api/incidents/{incidentId}/timeline", result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .WithName("AddTimelineEntry")
        .WithTags("Timeline")
        .Produces<TimelineEntryResponse>(201)
        .Produces(400)
        .Produces(404);

        // Note: No PUT/PATCH/DELETE endpoints - timeline entries are append-only
    }
}
