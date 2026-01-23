using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ShipSquire.Application.DTOs;
using ShipSquire.Domain.Enums;
using Xunit;

namespace ShipSquire.Tests.Integration.Endpoints;

/// <summary>
/// End-to-end tests for the incident export feature and full incident lifecycle.
/// </summary>
public class IncidentExportEndpointsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly HttpClient _otherUserClient;

    public IncidentExportEndpointsTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-User-Email", "export-test@example.com");

        _otherUserClient = factory.CreateClient();
        _otherUserClient.DefaultRequestHeaders.Add("X-User-Email", "other-export@example.com");
    }

    [Fact]
    public async Task ExportIncident_BasicIncident_ReturnsMarkdown()
    {
        // Arrange
        var service = await CreateTestService("Export Service 1", "export-svc-1");
        var incident = await CreateTestIncident(service.Id, "Database Connection Timeout");

        // Act
        var response = await _client.GetAsync($"/api/incidents/{incident.Id}/export");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/markdown");

        var markdown = await response.Content.ReadAsStringAsync();
        markdown.Should().Contain("# Incident Report: Database Connection Timeout");
        markdown.Should().Contain("## Overview");
        markdown.Should().Contain("| **Service** | Export Service 1 |");
        markdown.Should().Contain("| **Severity** | SEV3 |");
        markdown.Should().Contain("| **Status** | Open |");
        markdown.Should().Contain("## Timeline");
        markdown.Should().Contain("*No timeline entries recorded.*");
        markdown.Should().Contain("*Exported from ShipSquire on");
    }

    [Fact]
    public async Task ExportIncident_WithTimeline_IncludesTimelineEntries()
    {
        // Arrange
        var service = await CreateTestService("Export Timeline Service", "export-timeline-svc");
        var incident = await CreateTestIncident(service.Id, "API Latency Issue");

        // Add timeline entries
        await _client.PostAsJsonAsync($"/api/incidents/{incident.Id}/timeline",
            new TimelineEntryRequest(TimelineEntryType.Observation, "Noticed increased latency on API calls"));
        await _client.PostAsJsonAsync($"/api/incidents/{incident.Id}/timeline",
            new TimelineEntryRequest(TimelineEntryType.Action, "Scaled up API instances"));
        await _client.PostAsJsonAsync($"/api/incidents/{incident.Id}/timeline",
            new TimelineEntryRequest(TimelineEntryType.Decision, "Decided to enable rate limiting"));

        // Act
        var response = await _client.GetAsync($"/api/incidents/{incident.Id}/export");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var markdown = await response.Content.ReadAsStringAsync();
        markdown.Should().Contain("## Timeline");
        markdown.Should().Contain("👁️ Observation"); // Entry type indicator
        markdown.Should().Contain("Noticed increased latency on API calls");
        markdown.Should().Contain("⚡ Action");
        markdown.Should().Contain("Scaled up API instances");
        markdown.Should().Contain("🎯 Decision");
        markdown.Should().Contain("Decided to enable rate limiting");
        markdown.Should().NotContain("*No timeline entries recorded.*");
    }

    [Fact]
    public async Task ExportIncident_ResolvedWithPostmortem_IncludesPostmortem()
    {
        // Arrange
        var service = await CreateTestService("Export PM Service", "export-pm-svc");
        var incident = await CreateTestIncident(service.Id, "Production Outage");

        // Add timeline
        await _client.PostAsJsonAsync($"/api/incidents/{incident.Id}/timeline",
            new TimelineEntryRequest(TimelineEntryType.Observation, "Service health checks failing"));
        await _client.PostAsJsonAsync($"/api/incidents/{incident.Id}/timeline",
            new TimelineEntryRequest(TimelineEntryType.Action, "Restarted affected pods"));

        // Progress to resolved
        await _client.PostAsJsonAsync($"/api/incidents/{incident.Id}/status",
            new StatusTransitionRequest(IncidentStatus.Investigating));
        await _client.PostAsJsonAsync($"/api/incidents/{incident.Id}/status",
            new StatusTransitionRequest(IncidentStatus.Resolved));

        // Trigger postmortem generation
        await _client.GetAsync($"/api/incidents/{incident.Id}/postmortem");

        // Act
        var response = await _client.GetAsync($"/api/incidents/{incident.Id}/export");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var markdown = await response.Content.ReadAsStringAsync();
        markdown.Should().Contain("| **Status** | Resolved |");
        markdown.Should().Contain("| **Duration** |"); // Duration should be present
        markdown.Should().Contain("# Postmortem");
        markdown.Should().Contain("## Impact Summary");
        markdown.Should().Contain("## Root Cause Analysis");
        markdown.Should().Contain("## Detection");
        markdown.Should().Contain("## Resolution");
        markdown.Should().Contain("## Action Items");
    }

    [Fact]
    public async Task ExportIncident_SanitizesSecrets()
    {
        // Arrange
        var service = await CreateTestService("Export Secrets Service", "export-secrets-svc");
        var incidentRequest = new IncidentRequest(
            "API Key Exposure",
            IncidentSeverity.Sev2,
            DateTimeOffset.UtcNow,
            "Discovered api_key=sk-test-secret123 in logs"
        );
        var createResponse = await _client.PostAsJsonAsync($"/api/services/{service.Id}/incidents", incidentRequest);
        var incident = await createResponse.Content.ReadFromJsonAsync<IncidentResponse>();

        // Add timeline with secrets
        await _client.PostAsJsonAsync($"/api/incidents/{incident!.Id}/timeline",
            new TimelineEntryRequest(TimelineEntryType.Note, "Found password=supersecretpassword in config"));

        // Act
        var response = await _client.GetAsync($"/api/incidents/{incident.Id}/export");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var markdown = await response.Content.ReadAsStringAsync();
        markdown.Should().Contain("[REDACTED]");
        markdown.Should().NotContain("sk-test-secret123");
        markdown.Should().NotContain("supersecretpassword");
    }

    [Fact]
    public async Task ExportIncident_OtherUsersIncident_ReturnsNotFound()
    {
        // Arrange - Create as first user
        var service = await CreateTestService("Export Cross User Service", "export-cross-svc");
        var incident = await CreateTestIncident(service.Id, "Cross User Test");

        // Act - Try to export as different user
        var response = await _otherUserClient.GetAsync($"/api/incidents/{incident.Id}/export");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ExportIncident_NonExistent_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync($"/api/incidents/{Guid.NewGuid()}/export");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task FullIncidentLifecycle_EndToEnd_Success()
    {
        // This test covers the complete incident lifecycle:
        // 1. Create service
        // 2. Create incident
        // 3. Add timeline entries
        // 4. Transition through statuses
        // 5. Generate postmortem
        // 6. Export

        // Step 1: Create service
        var service = await CreateTestService("E2E Service", "e2e-svc");
        service.Should().NotBeNull();

        // Step 2: Create incident
        var incidentRequest = new IncidentRequest(
            "Production Database Slowdown",
            IncidentSeverity.Sev2,
            DateTimeOffset.UtcNow.AddMinutes(-30),
            "Database queries taking 10x longer than normal"
        );
        var incidentResponse = await _client.PostAsJsonAsync($"/api/services/{service.Id}/incidents", incidentRequest);
        incidentResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var incident = await incidentResponse.Content.ReadFromJsonAsync<IncidentResponse>();
        incident.Should().NotBeNull();
        incident!.Status.Should().Be(IncidentStatus.Open);

        // Step 3: Add timeline entries
        var entry1 = await _client.PostAsJsonAsync($"/api/incidents/{incident.Id}/timeline",
            new TimelineEntryRequest(TimelineEntryType.Observation, "Query response times spiked to 5s"));
        entry1.StatusCode.Should().Be(HttpStatusCode.Created);

        var entry2 = await _client.PostAsJsonAsync($"/api/incidents/{incident.Id}/timeline",
            new TimelineEntryRequest(TimelineEntryType.Action, "Identified long-running query in slow query log"));
        entry2.StatusCode.Should().Be(HttpStatusCode.Created);

        var entry3 = await _client.PostAsJsonAsync($"/api/incidents/{incident.Id}/timeline",
            new TimelineEntryRequest(TimelineEntryType.Decision, "Adding index to users table to optimize query"));
        entry3.StatusCode.Should().Be(HttpStatusCode.Created);

        var entry4 = await _client.PostAsJsonAsync($"/api/incidents/{incident.Id}/timeline",
            new TimelineEntryRequest(TimelineEntryType.Action, "Index created, queries now under 100ms"));
        entry4.StatusCode.Should().Be(HttpStatusCode.Created);

        // Verify timeline
        var timelineResponse = await _client.GetAsync($"/api/incidents/{incident.Id}/timeline");
        var timeline = await timelineResponse.Content.ReadFromJsonAsync<List<TimelineEntryResponse>>();
        timeline.Should().HaveCount(4);

        // Step 4: Transition statuses
        // Open -> Investigating
        var transition1 = await _client.PostAsJsonAsync($"/api/incidents/{incident.Id}/status",
            new StatusTransitionRequest(IncidentStatus.Investigating));
        transition1.StatusCode.Should().Be(HttpStatusCode.OK);

        // Investigating -> Mitigated
        var transition2 = await _client.PostAsJsonAsync($"/api/incidents/{incident.Id}/status",
            new StatusTransitionRequest(IncidentStatus.Mitigated));
        transition2.StatusCode.Should().Be(HttpStatusCode.OK);

        // Mitigated -> Resolved
        var transition3 = await _client.PostAsJsonAsync($"/api/incidents/{incident.Id}/status",
            new StatusTransitionRequest(IncidentStatus.Resolved));
        transition3.StatusCode.Should().Be(HttpStatusCode.OK);

        var finalTransition = await transition3.Content.ReadFromJsonAsync<StatusTransitionResponse>();
        finalTransition!.NewStatus.Should().Be(IncidentStatus.Resolved);
        finalTransition.EndedAt.Should().NotBeNull();

        // Step 5: Generate/Get postmortem
        var postmortemResponse = await _client.GetAsync($"/api/incidents/{incident.Id}/postmortem");
        postmortemResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var postmortem = await postmortemResponse.Content.ReadFromJsonAsync<PostmortemResponse>();
        postmortem.Should().NotBeNull();
        postmortem!.DetectionMarkdown.Should().Contain("Query response times spiked");
        postmortem.ResolutionMarkdown.Should().Contain("Index created");
        postmortem.RootCauseMarkdown.Should().Contain("Adding index");

        // Step 6: Export
        var exportResponse = await _client.GetAsync($"/api/incidents/{incident.Id}/export");
        exportResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var markdown = await exportResponse.Content.ReadAsStringAsync();
        markdown.Should().Contain("# Incident Report: Production Database Slowdown");
        markdown.Should().Contain("| **Severity** | SEV2 |");
        markdown.Should().Contain("| **Status** | Resolved |");
        markdown.Should().Contain("Database queries taking 10x longer than normal");
        markdown.Should().Contain("Query response times spiked to 5s");
        markdown.Should().Contain("Index created, queries now under 100ms");
        markdown.Should().Contain("# Postmortem");
    }

    [Fact]
    public async Task InvalidStatusTransition_ReturnsDetailedError()
    {
        // Arrange
        var service = await CreateTestService("Error Test Service", "error-test-svc");
        var incident = await CreateTestIncident(service.Id, "Error Test Incident");

        // Act - Try invalid transition: Open -> Mitigated (should go through Investigating first)
        var response = await _client.PostAsJsonAsync($"/api/incidents/{incident.Id}/status",
            new StatusTransitionRequest(IncidentStatus.Mitigated));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var error = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        error.Should().ContainKey("code");
        error.Should().ContainKey("message");
        error!["code"].ToString().Should().Be("INVALID_STATUS_TRANSITION");
        error["message"].ToString().Should().Contain("Cannot change status from 'open' to 'mitigated'");
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
