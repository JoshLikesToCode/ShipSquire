using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using ShipSquire.Infrastructure.Services;

namespace ShipSquire.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        // GitHub OAuth login - redirects to GitHub
        app.MapGet("/auth/github/login", (IConfiguration config) =>
        {
            var clientId = config["GitHub:ClientId"];
            var redirectUri = config["GitHub:RedirectUri"] ?? "http://localhost:5000/auth/github/callback";
            var scope = "user:email";

            var githubAuthUrl = $"https://github.com/login/oauth/authorize" +
                               $"?client_id={clientId}" +
                               $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                               $"&scope={Uri.EscapeDataString(scope)}";

            return Results.Redirect(githubAuthUrl);
        })
        .WithName("GitHubLogin")
        .WithTags("Auth")
        .Produces(302);

        // GitHub OAuth callback
        app.MapGet("/auth/github/callback", async (
            string? code,
            string? error,
            IGitHubOAuthService githubService,
            HttpContext context,
            IConfiguration config) =>
        {
            if (!string.IsNullOrEmpty(error))
            {
                // Redirect to frontend with error
                var frontendUrl = config["Frontend:Url"] ?? "http://localhost:3000";
                return Results.Redirect($"{frontendUrl}?error={Uri.EscapeDataString(error)}");
            }

            if (string.IsNullOrEmpty(code))
            {
                return Results.BadRequest(new { error = "Authorization code is required" });
            }

            try
            {
                // Exchange code for access token
                var accessToken = await githubService.ExchangeCodeForTokenAsync(code);

                // Get GitHub user info
                var githubUser = await githubService.GetGitHubUserAsync(accessToken);

                // Create or update user in our database
                var user = await githubService.CreateOrUpdateUserFromGitHubAsync(githubUser, accessToken);

                // Create authentication claims
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Name, user.DisplayName ?? user.Email),
                    new Claim("AuthProvider", "github")
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                // Sign in with cookie authentication
                await context.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    claimsPrincipal,
                    new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
                    });

                // Redirect to frontend
                var frontendUrl = config["Frontend:Url"] ?? "http://localhost:3000";
                return Results.Redirect($"{frontendUrl}/services");
            }
            catch (Exception ex)
            {
                // Log error and redirect to frontend with error
                var frontendUrl = config["Frontend:Url"] ?? "http://localhost:3000";
                return Results.Redirect($"{frontendUrl}?error={Uri.EscapeDataString(ex.Message)}");
            }
        })
        .WithName("GitHubCallback")
        .WithTags("Auth")
        .Produces(302)
        .Produces(400);

        // Logout endpoint
        app.MapPost("/auth/logout", async (HttpContext context) =>
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Ok(new { message = "Logged out successfully" });
        })
        .WithName("Logout")
        .WithTags("Auth")
        .Produces(200);

        // Get current user (for frontend to check auth status)
        app.MapGet("/auth/me", (HttpContext context) =>
        {
            if (!context.User.Identity?.IsAuthenticated ?? false)
            {
                return Results.Unauthorized();
            }

            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = context.User.FindFirst(ClaimTypes.Email)?.Value;
            var name = context.User.FindFirst(ClaimTypes.Name)?.Value;
            var authProvider = context.User.FindFirst("AuthProvider")?.Value;

            return Results.Ok(new
            {
                id = userId,
                email = email,
                displayName = name,
                authProvider = authProvider,
                isAuthenticated = true
            });
        })
        .WithName("GetCurrentAuthUser")
        .WithTags("Auth")
        .Produces(200)
        .Produces(401);
    }
}
