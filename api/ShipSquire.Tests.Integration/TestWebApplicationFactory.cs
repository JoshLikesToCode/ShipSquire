using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShipSquire.Infrastructure.Persistence;

namespace ShipSquire.Tests.Integration;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"InMemoryTestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the app's DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ShipSquireDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add DbContext using a shared in-memory database for all requests in this factory
            services.AddDbContext<ShipSquireDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
            });

            // Build the service provider
            var sp = services.BuildServiceProvider();

            // Create a scope to obtain a reference to the database context
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<ShipSquireDbContext>();

            // Ensure the database is created
            db.Database.EnsureCreated();
        });
    }
}
