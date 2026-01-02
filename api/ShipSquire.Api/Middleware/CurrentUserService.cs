using ShipSquire.Application.Interfaces;

namespace ShipSquire.Api.Middleware;

public class CurrentUserService : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId
    {
        get
        {
            var currentUser = _httpContextAccessor.HttpContext?.Items["CurrentUser"] as ICurrentUser;
            return currentUser?.UserId ?? Guid.Empty;
        }
    }

    public string Email
    {
        get
        {
            var currentUser = _httpContextAccessor.HttpContext?.Items["CurrentUser"] as ICurrentUser;
            return currentUser?.Email ?? "unknown";
        }
    }
}
