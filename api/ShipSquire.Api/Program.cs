using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using ShipSquire.Api.Endpoints;
using ShipSquire.Api.Middleware;
using ShipSquire.Application.Interfaces;
using ShipSquire.Application.Services;
using ShipSquire.Domain.Entities;
using ShipSquire.Domain.Interfaces;
using ShipSquire.Infrastructure.Persistence;
using ShipSquire.Infrastructure.Repositories;
using ShipSquire.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();

// Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "ShipSquire.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = 401;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();

// Add CORS - Allow credentials for cookie authentication
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var frontendUrl = builder.Configuration["Frontend:Url"] ?? "http://localhost:3000";
        policy.WithOrigins(frontendUrl)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // Required for cookies
    });
});

// Database - Prioritize DATABASE_URL env var (12-factor app), then fallback to appsettings
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Database=shipSquire;Username=postgres;Password=postgres";

builder.Services.AddDbContext<ShipSquireDbContext>(options =>
    options.UseNpgsql(connectionString));

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IServiceRepository, ServiceRepository>();
builder.Services.AddScoped<IRunbookRepository, RunbookRepository>();
builder.Services.AddScoped<IIncidentRepository, IncidentRepository>();
builder.Services.AddScoped<ITimelineEntryRepository, TimelineEntryRepository>();
builder.Services.AddScoped<IPostmortemRepository, PostmortemRepository>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Application Services
builder.Services.AddScoped<ICurrentUser, CurrentUserService>();
builder.Services.AddScoped<ServiceService>();
builder.Services.AddScoped<RunbookService>();
builder.Services.AddScoped<RunbookSectionService>();
builder.Services.AddScoped<RunbookVariableService>();
builder.Services.AddScoped<IncidentService>();
builder.Services.AddScoped<TimelineEntryService>();
builder.Services.AddScoped<PostmortemService>();
builder.Services.AddScoped<MarkdownExportService>();
builder.Services.AddScoped<IRunbookDraftGenerator, RunbookDraftGenerator>();

// Auth Services
var encryptionKey = builder.Configuration["Encryption:Key"] ?? throw new InvalidOperationException("Encryption:Key not configured");
builder.Services.AddSingleton<ITokenEncryptionService>(new TokenEncryptionService(encryptionKey));
builder.Services.AddScoped<IGitHubOAuthService, GitHubOAuthService>();
builder.Services.AddScoped<IGitHubApiClient, GitHubApiClient>();
builder.Services.AddScoped<IRepoAnalyzer, GitHubRepoAnalyzer>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseCurrentUser();

// Map endpoints
app.MapHealthEndpoints();
app.MapAuthEndpoints();
app.MapGitHubEndpoints();
app.MapUserEndpoints();
app.MapServiceEndpoints();
app.MapRunbookEndpoints();
app.MapRunbookSectionEndpoints();
app.MapRunbookVariableEndpoints();
app.MapIncidentEndpoints();
app.MapTimelineEndpoints();
app.MapPostmortemEndpoints();

// Run migrations on startup (only for relational databases)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ShipSquireDbContext>();
    if (db.Database.IsRelational())
    {
        db.Database.Migrate();
    }
    else
    {
        db.Database.EnsureCreated();
    }
}

app.Run();

// Make Program accessible for testing
public partial class Program { }
