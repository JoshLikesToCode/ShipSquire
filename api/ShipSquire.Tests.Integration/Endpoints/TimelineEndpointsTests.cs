using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ShipSquire.Application.DTOs;
using ShipSquire.Domain.Enums;
using Xunit;

namespace ShipSquire.Tests.Integration.Endpoints;

public class TimelineEndpointsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly HttpClient _otherUserClient;

    public TimelineEndpointsTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-User-Email", "timeline-test@example.com");

        _otherUserClient = factory.CreateClient();
        _otherUserClient.DefaultRequestHeaders.Add("X-User-Email", "other-timeline@example.com");
    }

    [Fact]
    public async Task AddTimelineEntry_ShouldCreateEntry()
    {
        // Arrange
        var service = await CreateTestService("Timeline Service 1", "timeline-svc-1");
        var incident = await CreateTestIncident(service.Id, "Timeline Incident 1");

        var request = new TimelineEntryRequest(
            EntryType: TimelineEntryType.Note,
            BodyMarkdown: "Initial investigation started"
        );

        // Act
        var response = await _client.PostAsJsonAsync($"/api/incidents/{incident.Id}/timeline", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var entry = await response.Content.ReadFromJsonAsync<TimelineEntryResponse>();
        entry.Should().NotBeNull();
        entry!.EntryType.Should().Be(TimelineEntryType.Note);
        entry.BodyMarkdown.Should().Be("Initial investigation started");
        entry.IncidentId.Should().Be(incident.Id);
        entry.OccurredAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(TimelineEntryType.Note)]
    [InlineData(TimelineEntryType.Action)]
    [InlineData(TimelineEntryType.Decision)]
    [InlineData(TimelineEntryType.Observation)]
    public async Task AddTimelineEntry_WithAllTypes_ShouldSucceed(string entryType)
    {
        // Arrange
        var service = await CreateTestService($"Timeline Svc {entryType}", $"timeline-svc-{entryType}");
        var incident = await CreateTestIncident(service.Id, $"Incident {entryType}");

        var request = new TimelineEntryRequest(entryType, $"Test entry of type {entryType}");

        // Act
        var response = await _client.PostAsJsonAsync($"/api/incidents/{incident.Id}/timeline", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var entry = await response.Content.ReadFromJsonAsync<TimelineEntryResponse>();
        entry!.EntryType.Should().Be(entryType);
    }

    [Fact]
    public async Task AddTimelineEntry_WithInvalidType_ShouldReturnBadRequest()
    {
        // Arrange
        var service = await CreateTestService("Bad Type Service", "bad-type-svc");
        var incident = await CreateTestIncident(service.Id, "Bad Type Incident");

        var request = new { EntryType = "invalid_type", BodyMarkdown = "Test" };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/incidents/{incident.Id}/timeline", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddTimelineEntry_WithEmptyBody_ShouldReturnBadRequest()
    {
        // Arrange
        var service = await CreateTestService("Empty Body Service", "empty-body-svc");
        var incident = await CreateTestIncident(service.Id, "Empty Body Incident");

        var request = new TimelineEntryRequest(TimelineEntryType.Note, "");

        // Act
        var response = await _client.PostAsJsonAsync($"/api/incidents/{incident.Id}/timeline", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetTimeline_ShouldReturnEntriesInOrder()
    {
        // Arrange
        var service = await CreateTestService("Ordered Timeline Service", "ordered-timeline");
        var incident = await CreateTestIncident(service.Id, "Ordered Incident");

        // Add entries in quick succession
        await _client.PostAsJsonAsync($"/api/incidents/{incident.Id}/timeline",
            new TimelineEntryRequest(TimelineEntryType.Note, "First entry"));
        await Task.Delay(50); // Small delay to ensure different timestamps
        await _client.PostAsJsonAsync($"/api/incidents/{incident.Id}/timeline",
            new TimelineEntryRequest(TimelineEntryType.Action, "Second entry"));
        await Task.Delay(50);
        await _client.PostAsJsonAsync($"/api/incidents/{incident.Id}/timeline",
            new TimelineEntryRequest(TimelineEntryType.Decision, "Third entry"));

        // Act
        var response = await _client.GetAsync($"/api/incidents/{incident.Id}/timeline");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var entries = await response.Content.ReadFromJsonAsync<List<TimelineEntryResponse>>();
        entries.Should().HaveCount(3);

        // Entries should be in chronological order
        entries![0].BodyMarkdown.Should().Be("First entry");
        entries[1].BodyMarkdown.Should().Be("Second entry");
        entries[2].BodyMarkdown.Should().Be("Third entry");

        // OccurredAt timestamps should be in order
        entries[0].OccurredAt.Should().BeBefore(entries[1].OccurredAt);
        entries[1].OccurredAt.Should().BeBefore(entries[2].OccurredAt);
    }

    [Fact]
    public async Task GetTimeline_WithNonExistentIncident_ShouldReturnEmpty()
    {
        // Arrange
        var fakeIncidentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/incidents/{fakeIncidentId}/timeline");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var entries = await response.Content.ReadFromJsonAsync<List<TimelineEntryResponse>>();
        entries.Should().BeEmpty();
    }

    [Fact]
    public async Task AddTimelineEntry_ToOtherUsersIncident_ShouldReturnNotFound()
    {
        // Arrange - Create incident as first user
        var service = await CreateTestService("Cross User Timeline", "cross-user-timeline");
        var incident = await CreateTestIncident(service.Id, "Protected Incident");

        var request = new TimelineEntryRequest(TimelineEntryType.Note, "Attempted entry");

        // Act - Try to add entry as different user
        var response = await _otherUserClient.PostAsJsonAsync($"/api/incidents/{incident.Id}/timeline", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTimeline_OfOtherUsersIncident_ShouldReturnEmpty()
    {
        // Arrange - Create incident with entries as first user
        var service = await CreateTestService("Cross User Get Timeline", "cross-user-get-timeline");
        var incident = await CreateTestIncident(service.Id, "Protected Get Incident");

        await _client.PostAsJsonAsync($"/api/incidents/{incident.Id}/timeline",
            new TimelineEntryRequest(TimelineEntryType.Note, "Private entry"));

        // Act - Try to get timeline as different user
        var response = await _otherUserClient.GetAsync($"/api/incidents/{incident.Id}/timeline");

        // Assert - Should return empty (not expose other user's data)
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var entries = await response.Content.ReadFromJsonAsync<List<TimelineEntryResponse>>();
        entries.Should().BeEmpty();
    }

    [Fact]
    public async Task TimelineEntries_AreAppendOnly_NoPutEndpoint()
    {
        // Arrange
        var service = await CreateTestService("Append Only Service", "append-only-svc");
        var incident = await CreateTestIncident(service.Id, "Append Only Incident");

        var createResponse = await _client.PostAsJsonAsync($"/api/incidents/{incident.Id}/timeline",
            new TimelineEntryRequest(TimelineEntryType.Note, "Original content"));
        var entry = await createResponse.Content.ReadFromJsonAsync<TimelineEntryResponse>();

        // Act - Try to PUT (should not exist)
        var putRequest = new TimelineEntryRequest(TimelineEntryType.Note, "Modified content");
        var putResponse = await _client.PutAsJsonAsync($"/api/incidents/{incident.Id}/timeline/{entry!.Id}", putRequest);

        // Assert - PUT endpoint should not exist
        putResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TimelineEntries_AreAppendOnly_NoDeleteEndpoint()
    {
        // Arrange
        var service = await CreateTestService("No Delete Service", "no-delete-svc");
        var incident = await CreateTestIncident(service.Id, "No Delete Incident");

        var createResponse = await _client.PostAsJsonAsync($"/api/incidents/{incident.Id}/timeline",
            new TimelineEntryRequest(TimelineEntryType.Note, "Permanent entry"));
        var entry = await createResponse.Content.ReadFromJsonAsync<TimelineEntryResponse>();

        // Act - Try to DELETE (should not exist)
        var deleteResponse = await _client.DeleteAsync($"/api/incidents/{incident.Id}/timeline/{entry!.Id}");

        // Assert - DELETE endpoint should not exist
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TimelineEntries_PersistCorrectly()
    {
        // Arrange
        var service = await CreateTestService("Persist Service", "persist-svc");
        var incident = await CreateTestIncident(service.Id, "Persist Incident");

        // Add entry
        var createResponse = await _client.PostAsJsonAsync($"/api/incidents/{incident.Id}/timeline",
            new TimelineEntryRequest(TimelineEntryType.Action, "Restarted the service"));
        var createdEntry = await createResponse.Content.ReadFromJsonAsync<TimelineEntryResponse>();

        // Act - Fetch timeline
        var getResponse = await _client.GetAsync($"/api/incidents/{incident.Id}/timeline");
        var entries = await getResponse.Content.ReadFromJsonAsync<List<TimelineEntryResponse>>();

        // Assert - Entry should persist with all fields
        entries.Should().HaveCount(1);
        entries![0].Id.Should().Be(createdEntry!.Id);
        entries[0].EntryType.Should().Be(TimelineEntryType.Action);
        entries[0].BodyMarkdown.Should().Be("Restarted the service");
        entries[0].IncidentId.Should().Be(incident.Id);
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
