using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ShipSquire.Application.DTOs;
using ShipSquire.Domain.Enums;
using Xunit;

namespace ShipSquire.Tests.Integration.Endpoints;

public class PostmortemEndpointsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly HttpClient _otherUserClient;

    public PostmortemEndpointsTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-User-Email", "postmortem-test@example.com");

        _otherUserClient = factory.CreateClient();
        _otherUserClient.DefaultRequestHeaders.Add("X-User-Email", "other-postmortem@example.com");
    }

    [Fact]
    public async Task GetPostmortem_ForResolvedIncident_AutoGeneratesPostmortem()
    {
        // Arrange - Create service, incident, resolve it
        var service = await CreateTestService("PM Service 1", "pm-svc-1");
        var incident = await CreateTestIncident(service.Id, "PM Incident 1");

        // Progress through status: Open -> Investigating -> Resolved
        await _client.PostAsJsonAsync($"/api/incidents/{incident.Id}/status",
            new StatusTransitionRequest(IncidentStatus.Investigating));
        await _client.PostAsJsonAsync($"/api/incidents/{incident.Id}/status",
            new StatusTransitionRequest(IncidentStatus.Resolved));

        // Act
        var response = await _client.GetAsync($"/api/incidents/{incident.Id}/postmortem");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var postmortem = await response.Content.ReadFromJsonAsync<PostmortemResponse>();
        postmortem.Should().NotBeNull();
        postmortem!.IncidentId.Should().Be(incident.Id);
        postmortem.ImpactMarkdown.Should().Contain("Impact Summary");
        postmortem.RootCauseMarkdown.Should().Contain("Root Cause Analysis");
        postmortem.ResolutionMarkdown.Should().Contain("Resolution");
        postmortem.ActionItemsMarkdown.Should().Contain("Action Items");
    }

    [Fact]
    public async Task GetPostmortem_ForNonResolvedIncident_ReturnsNotFound()
    {
        // Arrange - Create service and incident (stays Open)
        var service = await CreateTestService("PM Service 2", "pm-svc-2");
        var incident = await CreateTestIncident(service.Id, "PM Incident 2");

        // Act - Try to get postmortem for non-resolved incident
        var response = await _client.GetAsync($"/api/incidents/{incident.Id}/postmortem");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPostmortem_IncludesTimelineEntriesInDraft()
    {
        // Arrange
        var service = await CreateTestService("PM Timeline Service", "pm-timeline-svc");
        var incident = await CreateTestIncident(service.Id, "PM Timeline Incident");

        // Add timeline entries
        await _client.PostAsJsonAsync($"/api/incidents/{incident.Id}/timeline",
            new TimelineEntryRequest(TimelineEntryType.Observation, "Noticed high CPU usage"));
        await _client.PostAsJsonAsync($"/api/incidents/{incident.Id}/timeline",
            new TimelineEntryRequest(TimelineEntryType.Action, "Scaled up instances"));
        await _client.PostAsJsonAsync($"/api/incidents/{incident.Id}/timeline",
            new TimelineEntryRequest(TimelineEntryType.Decision, "Decided to rollback"));

        // Resolve the incident
        await _client.PostAsJsonAsync($"/api/incidents/{incident.Id}/status",
            new StatusTransitionRequest(IncidentStatus.Investigating));
        await _client.PostAsJsonAsync($"/api/incidents/{incident.Id}/status",
            new StatusTransitionRequest(IncidentStatus.Resolved));

        // Act
        var response = await _client.GetAsync($"/api/incidents/{incident.Id}/postmortem");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var postmortem = await response.Content.ReadFromJsonAsync<PostmortemResponse>();

        // Observations should be in Detection section
        postmortem!.DetectionMarkdown.Should().Contain("high CPU usage");

        // Actions should be in Resolution section
        postmortem.ResolutionMarkdown.Should().Contain("Scaled up instances");

        // Decisions should be in Root Cause section
        postmortem.RootCauseMarkdown.Should().Contain("Decided to rollback");
    }

    [Fact]
    public async Task UpdatePostmortem_UpdatesSections()
    {
        // Arrange
        var service = await CreateTestService("PM Update Service", "pm-update-svc");
        var incident = await CreateTestIncident(service.Id, "PM Update Incident");

        // Resolve the incident
        await _client.PostAsJsonAsync($"/api/incidents/{incident.Id}/status",
            new StatusTransitionRequest(IncidentStatus.Investigating));
        await _client.PostAsJsonAsync($"/api/incidents/{incident.Id}/status",
            new StatusTransitionRequest(IncidentStatus.Resolved));

        // Get auto-generated postmortem
        await _client.GetAsync($"/api/incidents/{incident.Id}/postmortem");

        // Act - Update the postmortem
        var updateRequest = new PostmortemUpdateRequest(
            ImpactMarkdown: "## Updated Impact\n\n100 customers affected for 30 minutes.",
            RootCauseMarkdown: "## Root Cause\n\nDatabase connection pool exhaustion."
        );

        var response = await _client.PatchAsJsonAsync($"/api/incidents/{incident.Id}/postmortem", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await response.Content.ReadFromJsonAsync<PostmortemResponse>();
        updated!.ImpactMarkdown.Should().Contain("100 customers affected");
        updated.RootCauseMarkdown.Should().Contain("Database connection pool exhaustion");
    }

    [Fact]
    public async Task UpdatePostmortem_CreatesIfNotExists()
    {
        // Arrange
        var service = await CreateTestService("PM Create Service", "pm-create-svc");
        var incident = await CreateTestIncident(service.Id, "PM Create Incident");

        // Don't resolve - just try to update directly
        var updateRequest = new PostmortemUpdateRequest(
            ImpactMarkdown: "Custom impact description"
        );

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/incidents/{incident.Id}/postmortem", updateRequest);

        // Assert - Should create and update
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var postmortem = await response.Content.ReadFromJsonAsync<PostmortemResponse>();
        postmortem!.ImpactMarkdown.Should().Be("Custom impact description");
    }

    [Fact]
    public async Task GetPostmortem_OtherUsersIncident_ReturnsNotFound()
    {
        // Arrange - Create as first user
        var service = await CreateTestService("PM Cross User", "pm-cross-user");
        var incident = await CreateTestIncident(service.Id, "PM Cross Incident");

        // Resolve it
        await _client.PostAsJsonAsync($"/api/incidents/{incident.Id}/status",
            new StatusTransitionRequest(IncidentStatus.Investigating));
        await _client.PostAsJsonAsync($"/api/incidents/{incident.Id}/status",
            new StatusTransitionRequest(IncidentStatus.Resolved));

        // Act - Try to get as different user
        var response = await _otherUserClient.GetAsync($"/api/incidents/{incident.Id}/postmortem");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task StatusTransition_InvalidTransition_ReturnsBadRequest()
    {
        // Arrange
        var service = await CreateTestService("PM Status Service", "pm-status-svc");
        var incident = await CreateTestIncident(service.Id, "PM Status Incident");

        // Act - Try invalid transition: Open -> Mitigated (skipping Investigating)
        var response = await _client.PostAsJsonAsync($"/api/incidents/{incident.Id}/status",
            new StatusTransitionRequest(IncidentStatus.Mitigated));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var error = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        error!["message"].Should().Contain("Invalid status transition");
    }

    [Fact]
    public async Task StatusTransition_ToResolved_SetsEndedAt()
    {
        // Arrange
        var service = await CreateTestService("PM Resolved Service", "pm-resolved-svc");
        var incident = await CreateTestIncident(service.Id, "PM Resolved Incident");

        // Progress to Investigating
        await _client.PostAsJsonAsync($"/api/incidents/{incident.Id}/status",
            new StatusTransitionRequest(IncidentStatus.Investigating));

        // Act - Resolve
        var response = await _client.PostAsJsonAsync($"/api/incidents/{incident.Id}/status",
            new StatusTransitionRequest(IncidentStatus.Resolved));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var transition = await response.Content.ReadFromJsonAsync<StatusTransitionResponse>();
        transition!.NewStatus.Should().Be(IncidentStatus.Resolved);
        transition.EndedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task StatusTransition_Reopen_ClearsEndedAt()
    {
        // Arrange
        var service = await CreateTestService("PM Reopen Service", "pm-reopen-svc");
        var incident = await CreateTestIncident(service.Id, "PM Reopen Incident");

        // Resolve the incident
        await _client.PostAsJsonAsync($"/api/incidents/{incident.Id}/status",
            new StatusTransitionRequest(IncidentStatus.Investigating));
        await _client.PostAsJsonAsync($"/api/incidents/{incident.Id}/status",
            new StatusTransitionRequest(IncidentStatus.Resolved));

        // Act - Reopen
        var response = await _client.PostAsJsonAsync($"/api/incidents/{incident.Id}/status",
            new StatusTransitionRequest(IncidentStatus.Open));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var transition = await response.Content.ReadFromJsonAsync<StatusTransitionResponse>();
        transition!.NewStatus.Should().Be(IncidentStatus.Open);
        transition.EndedAt.Should().BeNull();
    }

    private async Task<ServiceResponse> CreateTestService(string name, string slug)
    {
        var request = new ServiceRequest(name, slug, null, null);
        var response = await _client.PostAsJsonAsync("/api/services", request);
        return (await response.Content.ReadFromJsonAsync<ServiceResponse>())!;
    }

    private async Task<IncidentResponse> CreateTestIncident(Guid serviceId, string title)
    {
        var request = new IncidentRequest(title, IncidentSeverity.Sev3, DateTimeOffset.UtcNow);
        var response = await _client.PostAsJsonAsync($"/api/services/{serviceId}/incidents", request);
        return (await response.Content.ReadFromJsonAsync<IncidentResponse>())!;
    }
}
