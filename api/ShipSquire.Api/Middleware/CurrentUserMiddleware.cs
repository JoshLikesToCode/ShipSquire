using ShipSquire.Application.Interfaces;
using ShipSquire.Domain.Interfaces;

namespace ShipSquire.Api.Middleware;

public class CurrentUserMiddleware
{
    private readonly RequestDelegate _next;

    public CurrentUserMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IUserRepository userRepository)
    {
        var email = context.Request.Headers["X-User-Email"].FirstOrDefault() ?? "josh@local";

        var user = await userRepository.GetOrCreateByEmailAsync(email);

        var currentUser = new CurrentUser(user.Id, user.Email);
        context.Items["CurrentUser"] = currentUser;

        await _next(context);
    }
}

public class CurrentUser : ICurrentUser
{
    public Guid UserId { get; }
    public string Email { get; }

    public CurrentUser(Guid userId, string email)
    {
        UserId = userId;
        Email = email;
    }
}

public static class CurrentUserMiddlewareExtensions
{
    public static IApplicationBuilder UseCurrentUser(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CurrentUserMiddleware>();
    }
}
