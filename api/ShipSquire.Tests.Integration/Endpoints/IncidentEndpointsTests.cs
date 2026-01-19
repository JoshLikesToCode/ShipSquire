using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ShipSquire.Application.DTOs;
using ShipSquire.Domain.Enums;
using Xunit;

namespace ShipSquire.Tests.Integration.Endpoints;

public class IncidentEndpointsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly HttpClient _otherUserClient;

    public IncidentEndpointsTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-User-Email", "test@example.com");

        _otherUserClient = factory.CreateClient();
        _otherUserClient.DefaultRequestHeaders.Add("X-User-Email", "other@example.com");
    }

    [Fact]
    public async Task CreateIncident_ShouldCreateWithOpenStatus()
    {
        // Arrange
        var service = await CreateTestService("Incident Service", "incident-service");

        var incidentRequest = new IncidentRequest(
            Title: "Production Outage",
            Severity: IncidentSeverity.Sev1,
            StartedAt: DateTimeOffset.UtcNow,
            SummaryMarkdown: "Database connection issues"
        );

        // Act
        var response = await _client.PostAsJsonAsync($"/api/services/{service.Id}/incidents", incidentRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var incident = await response.Content.ReadFromJsonAsync<IncidentResponse>();
        incident.Should().NotBeNull();
        incident!.Title.Should().Be("Production Outage");
        incident.Severity.Should().Be(IncidentSeverity.Sev1);
        incident.Status.Should().Be(IncidentStatus.Open);
        incident.ServiceId.Should().Be(service.Id);
        incident.SummaryMarkdown.Should().Be("Database connection issues");
    }

    [Fact]
    public async Task CreateIncident_WithRunbook_ShouldAutoAttachRunbook()
    {
        // Arrange
        var service = await CreateTestService("Runbook Service", "runbook-service");
        var runbook = await CreateTestRunbook(service.Id, "Deployment Runbook");

        var incidentRequest = new IncidentRequest(
            Title: "Deployment Failed",
            Severity: IncidentSeverity.Sev2,
            StartedAt: DateTimeOffset.UtcNow
        );

        // Act
        var response = await _client.PostAsJsonAsync($"/api/services/{service.Id}/incidents", incidentRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var incident = await response.Content.ReadFromJsonAsync<IncidentResponse>();
        incident.Should().NotBeNull();
        incident!.RunbookId.Should().Be(runbook.Id);
        incident.RunbookTitle.Should().Be("Deployment Runbook");
    }

    [Fact]
    public async Task CreateIncident_WithInvalidSeverity_ShouldReturnBadRequest()
    {
        // Arrange
        var service = await CreateTestService("Bad Severity Service", "bad-severity");

        var incidentRequest = new
        {
            Title = "Test Incident",
            Severity = "invalid",
            StartedAt = DateTimeOffset.UtcNow
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/services/{service.Id}/incidents", incidentRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateIncident_WithNonExistentService_ShouldReturnNotFound()
    {
        // Arrange
        var fakeServiceId = Guid.NewGuid();
        var incidentRequest = new IncidentRequest(
            Title: "Test Incident",
            Severity: IncidentSeverity.Sev3,
            StartedAt: DateTimeOffset.UtcNow
        );

        // Act
        var response = await _client.PostAsJsonAsync($"/api/services/{fakeServiceId}/incidents", incidentRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetIncident_ShouldReturnIncident()
    {
        // Arrange
        var service = await CreateTestService("Get Incident Service", "get-incident");
        var created = await CreateTestIncident(service.Id, "Test Incident");

        // Act
        var response = await _client.GetAsync($"/api/incidents/{created.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var incident = await response.Content.ReadFromJsonAsync<IncidentResponse>();
        incident.Should().NotBeNull();
        incident!.Id.Should().Be(created.Id);
        incident.Title.Should().Be("Test Incident");
    }

    [Fact]
    public async Task GetIncident_ByOtherUser_ShouldReturnNotFound()
    {
        // Arrange - Create service and incident as first user
        var service = await CreateTestService("Cross User Service", "cross-user");
        var incident = await CreateTestIncident(service.Id, "Private Incident");

        // Act - Try to access as different user
        var response = await _otherUserClient.GetAsync($"/api/incidents/{incident.Id}");

        // Assert - Should return 404 (not 403) to not leak existence
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListIncidents_ShouldReturnServiceIncidents()
    {
        // Arrange
        var service = await CreateTestService("List Incidents Service", "list-incidents");
        await CreateTestIncident(service.Id, "Incident 1");
        await CreateTestIncident(service.Id, "Incident 2");
        await CreateTestIncident(service.Id, "Incident 3");

        // Act
        var response = await _client.GetAsync($"/api/services/{service.Id}/incidents");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var incidents = await response.Content.ReadFromJsonAsync<List<IncidentResponse>>();
        incidents.Should().NotBeNull();
        incidents!.Count.Should().Be(3);
    }

    [Fact]
    public async Task ListIncidents_ByOtherUser_ShouldReturnEmpty()
    {
        // Arrange - Create service and incidents as first user
        var service = await CreateTestService("Other User List Service", "other-user-list");
        await CreateTestIncident(service.Id, "Incident 1");

        // Act - Try to list as different user
        var response = await _otherUserClient.GetAsync($"/api/services/{service.Id}/incidents");

        // Assert - Should return empty list (service not owned by this user)
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var incidents = await response.Content.ReadFromJsonAsync<List<IncidentResponse>>();
        incidents.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateIncident_ShouldUpdateFields()
    {
        // Arrange
        var service = await CreateTestService("Update Service", "update-service");
        var incident = await CreateTestIncident(service.Id, "Original Title");

        var updateRequest = new IncidentUpdateRequest(
            Title: "Updated Title",
            Status: IncidentStatus.Investigating
        );

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/incidents/{incident.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await response.Content.ReadFromJsonAsync<IncidentResponse>();
        updated.Should().NotBeNull();
        updated!.Title.Should().Be("Updated Title");
        updated.Status.Should().Be(IncidentStatus.Investigating);
    }

    [Fact]
    public async Task DeleteIncident_ShouldRemoveIncident()
    {
        // Arrange
        var service = await CreateTestService("Delete Service", "delete-service");
        var incident = await CreateTestIncident(service.Id, "To Be Deleted");

        // Act
        var response = await _client.DeleteAsync($"/api/incidents/{incident.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify it's gone
        var getResponse = await _client.GetAsync($"/api/incidents/{incident.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteIncident_ByOtherUser_ShouldReturnNotFound()
    {
        // Arrange
        var service = await CreateTestService("Delete Other Service", "delete-other");
        var incident = await CreateTestIncident(service.Id, "Protected Incident");

        // Act - Try to delete as different user
        var response = await _otherUserClient.DeleteAsync($"/api/incidents/{incident.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<ServiceResponse> CreateTestService(string name, string slug)
    {
        var request = new ServiceRequest(name, slug, null, null);
        var response = await _client.PostAsJsonAsync("/api/services", request);
        return (await response.Content.ReadFromJsonAsync<ServiceResponse>())!;
    }

    private async Task<RunbookResponse> CreateTestRunbook(Guid serviceId, string title)
    {
        var request = new RunbookRequest(title, null);
        var response = await _client.PostAsJsonAsync($"/api/services/{serviceId}/runbooks", request);
        return (await response.Content.ReadFromJsonAsync<RunbookResponse>())!;
    }

    private async Task<IncidentResponse> CreateTestIncident(Guid serviceId, string title)
    {
        var request = new IncidentRequest(title, IncidentSeverity.Sev3, DateTimeOffset.UtcNow);
        var response = await _client.PostAsJsonAsync($"/api/services/{serviceId}/incidents", request);
        return (await response.Content.ReadFromJsonAsync<IncidentResponse>())!;
    }
}
