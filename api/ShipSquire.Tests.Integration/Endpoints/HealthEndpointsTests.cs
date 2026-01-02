using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace ShipSquire.Tests.Integration.Endpoints;

public class HealthEndpointsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointsTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/api/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<HealthResponse>();
        result.Should().NotBeNull();
        result!.Status.Should().Be("ok");
    }

    private record HealthResponse(string Status);
}
