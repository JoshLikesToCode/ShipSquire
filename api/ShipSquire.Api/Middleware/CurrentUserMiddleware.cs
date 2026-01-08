using System.Security.Claims;
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
        Guid? userId = null;
        string? email = null;

        // Priority 1: Check if user is authenticated via cookie
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var emailClaim = context.User.FindFirst(ClaimTypes.Email)?.Value;

            if (Guid.TryParse(userIdClaim, out var parsedUserId))
            {
                userId = parsedUserId;
                email = emailClaim;
            }
        }

        // Priority 2: Fall back to X-User-Email header (for backward compatibility and testing)
        if (userId == null)
        {
            email = context.Request.Headers["X-User-Email"].FirstOrDefault() ?? "josh@local";
            var user = await userRepository.GetOrCreateByEmailAsync(email);
            userId = user.Id;
            email = user.Email;
        }

        if (userId.HasValue && !string.IsNullOrEmpty(email))
        {
            var currentUser = new CurrentUser(userId.Value, email);
            context.Items["CurrentUser"] = currentUser;
        }

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
