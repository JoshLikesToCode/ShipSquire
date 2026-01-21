using ShipSquire.Application.DTOs;
using ShipSquire.Application.Services;

namespace ShipSquire.Api.Endpoints;

public static class PostmortemEndpoints
{
    public static void MapPostmortemEndpoints(this IEndpointRouteBuilder app)
    {
        // Get postmortem for an incident (auto-generates if incident is resolved)
        app.MapGet("/api/incidents/{incidentId:guid}/postmortem", async (Guid incidentId, PostmortemService service) =>
        {
            var result = await service.GetByIncidentIdAsync(incidentId);
            return result == null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("GetPostmortem")
        .WithTags("Postmortems")
        .Produces<PostmortemResponse>(200)
        .Produces(404);

        // Update postmortem for an incident
        app.MapPatch("/api/incidents/{incidentId:guid}/postmortem", async (Guid incidentId, PostmortemUpdateRequest request, PostmortemService service) =>
        {
            var result = await service.UpdateAsync(incidentId, request);
            return result == null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("UpdatePostmortem")
        .WithTags("Postmortems")
        .Produces<PostmortemResponse>(200)
        .Produces(404);
    }
}
