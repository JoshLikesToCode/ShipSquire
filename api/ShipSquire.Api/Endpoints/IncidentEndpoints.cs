using ShipSquire.Application.DTOs;
using ShipSquire.Application.Services;

namespace ShipSquire.Api.Endpoints;

public static class IncidentEndpoints
{
    public static void MapIncidentEndpoints(this IEndpointRouteBuilder app)
    {
        // Get incidents for a service
        app.MapGet("/api/services/{serviceId:guid}/incidents", async (Guid serviceId, IncidentService service) =>
        {
            var incidents = await service.GetByServiceIdAsync(serviceId);
            return Results.Ok(incidents);
        })
        .WithName("GetServiceIncidents")
        .WithTags("Incidents")
        .Produces<IEnumerable<IncidentResponse>>(200);

        // Create incident for a service
        app.MapPost("/api/services/{serviceId:guid}/incidents", async (Guid serviceId, IncidentRequest request, IncidentService service) =>
        {
            try
            {
                var result = await service.CreateAsync(serviceId, request);
                return result == null
                    ? Results.NotFound(new { message = "Service not found" })
                    : Results.Created($"/api/incidents/{result.Id}", result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .WithName("CreateIncident")
        .WithTags("Incidents")
        .Produces<IncidentResponse>(201)
        .Produces(400)
        .Produces(404);

        // Get incident by ID
        app.MapGet("/api/incidents/{incidentId:guid}", async (Guid incidentId, IncidentService service) =>
        {
            var result = await service.GetByIdAsync(incidentId);
            return result == null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("GetIncident")
        .WithTags("Incidents")
        .Produces<IncidentResponse>(200)
        .Produces(404);

        // Update incident
        app.MapPatch("/api/incidents/{incidentId:guid}", async (Guid incidentId, IncidentUpdateRequest request, IncidentService service) =>
        {
            try
            {
                var result = await service.UpdateAsync(incidentId, request);
                return result == null ? Results.NotFound() : Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .WithName("UpdateIncident")
        .WithTags("Incidents")
        .Produces<IncidentResponse>(200)
        .Produces(400)
        .Produces(404);

        // Delete incident
        app.MapDelete("/api/incidents/{incidentId:guid}", async (Guid incidentId, IncidentService service) =>
        {
            var deleted = await service.DeleteAsync(incidentId);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteIncident")
        .WithTags("Incidents")
        .Produces(204)
        .Produces(404);
    }
}
