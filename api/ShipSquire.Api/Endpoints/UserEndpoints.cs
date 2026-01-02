using ShipSquire.Application.DTOs;
using ShipSquire.Application.Interfaces;
using ShipSquire.Domain.Interfaces;

namespace ShipSquire.Api.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users/me", async (ICurrentUser currentUser, IUserRepository userRepository) =>
        {
            var user = await userRepository.GetByIdAsync(currentUser.UserId);
            if (user == null) return Results.NotFound();

            var response = new UserResponse(
                user.Id,
                user.Email,
                user.DisplayName,
                user.CreatedAt,
                user.UpdatedAt
            );

            return Results.Ok(response);
        })
        .WithName("GetCurrentUser")
        .WithTags("Users")
        .Produces<UserResponse>(200)
        .Produces(404);
    }
}
