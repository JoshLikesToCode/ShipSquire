using Microsoft.EntityFrameworkCore;
using ShipSquire.Api.Endpoints;
using ShipSquire.Api.Middleware;
using ShipSquire.Application.Interfaces;
using ShipSquire.Application.Services;
using ShipSquire.Domain.Entities;
using ShipSquire.Domain.Interfaces;
using ShipSquire.Infrastructure.Persistence;
using ShipSquire.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
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
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Application Services
builder.Services.AddScoped<ICurrentUser, CurrentUserService>();
builder.Services.AddScoped<ServiceService>();
builder.Services.AddScoped<RunbookService>();
builder.Services.AddScoped<RunbookSectionService>();
builder.Services.AddScoped<RunbookVariableService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseCurrentUser();

// Map endpoints
app.MapHealthEndpoints();
app.MapUserEndpoints();
app.MapServiceEndpoints();
app.MapRunbookEndpoints();
app.MapRunbookSectionEndpoints();
app.MapRunbookVariableEndpoints();

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
