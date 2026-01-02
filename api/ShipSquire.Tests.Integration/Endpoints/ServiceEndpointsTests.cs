using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ShipSquire.Application.DTOs;
using Xunit;

namespace ShipSquire.Tests.Integration.Endpoints;

public class ServiceEndpointsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ServiceEndpointsTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-User-Email", "test@example.com");
    }

    [Fact]
    public async Task CreateService_ShouldReturnCreated()
    {
        // Arrange
        var request = new ServiceRequest(
            Name: "Test Service",
            Slug: "test-service",
            Description: "A test service",
            Repo: null
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/services", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<ServiceResponse>();
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Service");
        result.Slug.Should().Be("test-service");
    }

    [Fact]
    public async Task GetServices_ShouldReturnOk()
    {
        // Arrange
        var createRequest = new ServiceRequest(
            Name: "Test Service",
            Slug: "test-service",
            Description: "A test service",
            Repo: null
        );
        await _client.PostAsJsonAsync("/api/services", createRequest);

        // Act
        var response = await _client.GetAsync("/api/services");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<ServiceResponse>>();
        result.Should().NotBeNull();
        result!.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetService_WithWrongUser_ShouldReturnNotFound()
    {
        // Arrange
        var createRequest = new ServiceRequest(
            Name: "Test Service",
            Slug: "test-service",
            Description: "A test service",
            Repo: null
        );
        var createResponse = await _client.PostAsJsonAsync("/api/services", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ServiceResponse>();

        // Create a NEW client with a different user
        var otherClient = _factory.CreateClient();
        otherClient.DefaultRequestHeaders.Remove("X-User-Email");
        otherClient.DefaultRequestHeaders.Add("X-User-Email", "other@example.com");

        // Act
        var response = await otherClient.GetAsync($"/api/services/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
