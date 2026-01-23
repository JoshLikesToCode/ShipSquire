using ShipSquire.Application.DTOs;
using ShipSquire.Application.Exceptions;
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
                    ? Results.NotFound(new { message = "Service not found. Please check the service ID and try again." })
                    : Results.Created($"/api/incidents/{result.Id}", result);
            }
            catch (ValidationException ex)
            {
                return Results.BadRequest(new { code = ex.ErrorCode, field = ex.FieldName, message = ex.UserMessage });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { code = "VALIDATION_ERROR", message = ex.Message });
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
                return result == null
                    ? Results.NotFound(new { message = "Incident not found." })
                    : Results.Ok(result);
            }
            catch (InvalidStatusTransitionException ex)
            {
                return Results.BadRequest(new {
                    code = ex.ErrorCode,
                    message = ex.UserMessage,
                    currentStatus = ex.CurrentStatus,
                    requestedStatus = ex.RequestedStatus,
                    validTransitions = ex.ValidTransitions
                });
            }
            catch (ValidationException ex)
            {
                return Results.BadRequest(new { code = ex.ErrorCode, field = ex.FieldName, message = ex.UserMessage });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { code = "VALIDATION_ERROR", message = ex.Message });
            }
        })
        .WithName("UpdateIncident")
        .WithTags("Incidents")
        .Produces<IncidentResponse>(200)
        .Produces(400)
        .Produces(404);

        // Transition incident status
        app.MapPost("/api/incidents/{incidentId:guid}/status", async (Guid incidentId, StatusTransitionRequest request, IncidentService service) =>
        {
            try
            {
                var result = await service.TransitionStatusAsync(incidentId, request);
                return result == null
                    ? Results.NotFound(new { message = "Incident not found." })
                    : Results.Ok(result);
            }
            catch (InvalidStatusTransitionException ex)
            {
                return Results.BadRequest(new {
                    code = ex.ErrorCode,
                    message = ex.UserMessage,
                    currentStatus = ex.CurrentStatus,
                    requestedStatus = ex.RequestedStatus,
                    validTransitions = ex.ValidTransitions
                });
            }
            catch (ValidationException ex)
            {
                return Results.BadRequest(new { code = ex.ErrorCode, field = ex.FieldName, message = ex.UserMessage });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { code = "VALIDATION_ERROR", message = ex.Message });
            }
        })
        .WithName("TransitionIncidentStatus")
        .WithTags("Incidents")
        .Produces<StatusTransitionResponse>(200)
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

        // Export incident as Markdown
        app.MapGet("/api/incidents/{incidentId:guid}/export", async (Guid incidentId, MarkdownExportService exportService) =>
        {
            var result = await exportService.ExportIncidentAsync(incidentId);
            if (result == null)
                return Results.NotFound(new { message = "Incident not found" });

            return Results.Text(result.Content, result.ContentType, System.Text.Encoding.UTF8);
        })
        .WithName("ExportIncident")
        .WithTags("Incidents")
        .Produces<string>(200, "text/markdown")
        .Produces(404);
    }
}
