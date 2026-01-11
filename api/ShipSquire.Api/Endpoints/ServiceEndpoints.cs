using ShipSquire.Application.DTOs;
using ShipSquire.Application.Services;

namespace ShipSquire.Api.Endpoints;

public static class ServiceEndpoints
{
    public static void MapServiceEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/services", async (ServiceService service) =>
        {
            var services = await service.GetAllAsync();
            return Results.Ok(services);
        })
        .WithName("GetServices")
        .WithTags("Services")
        .Produces<IEnumerable<ServiceResponse>>(200);

        app.MapGet("/api/services/{serviceId:guid}", async (Guid serviceId, ServiceService service) =>
        {
            var result = await service.GetByIdAsync(serviceId);
            return result == null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("GetService")
        .WithTags("Services")
        .Produces<ServiceResponse>(200)
        .Produces(404);

        app.MapPost("/api/services", async (ServiceRequest request, ServiceService service) =>
        {
            var result = await service.CreateAsync(request);
            return Results.Created($"/api/services/{result.Id}", result);
        })
        .WithName("CreateService")
        .WithTags("Services")
        .Produces<ServiceResponse>(201);

        app.MapPatch("/api/services/{serviceId:guid}", async (Guid serviceId, ServiceRequest request, ServiceService service) =>
        {
            var result = await service.UpdateAsync(serviceId, request);
            return result == null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("UpdateService")
        .WithTags("Services")
        .Produces<ServiceResponse>(200)
        .Produces(404);

        app.MapDelete("/api/services/{serviceId:guid}", async (Guid serviceId, ServiceService service) =>
        {
            var deleted = await service.DeleteAsync(serviceId);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteService")
        .WithTags("Services")
        .Produces(204)
        .Produces(404);

        app.MapPatch("/api/services/{serviceId:guid}/link-repo", async (Guid serviceId, LinkRepoRequest request, ServiceService service) =>
        {
            var result = await service.LinkRepoAsync(serviceId, request);
            return result == null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("LinkRepositoryToService")
        .WithTags("Services")
        .Produces<ServiceResponse>(200)
        .Produces(404);
    }
}
