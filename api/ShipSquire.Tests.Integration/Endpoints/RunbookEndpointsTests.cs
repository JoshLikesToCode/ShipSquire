using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ShipSquire.Application.DTOs;
using Xunit;

namespace ShipSquire.Tests.Integration.Endpoints;

public class RunbookEndpointsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RunbookEndpointsTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-User-Email", "test@example.com");
    }

    [Fact]
    public async Task CreateRunbook_ShouldAutoSeedSections()
    {
        // Arrange - Create a service first
        var serviceRequest = new ServiceRequest(
            Name: "Test Service",
            Slug: "test-service",
            Description: "A test service",
            Repo: null
        );
        var serviceResponse = await _client.PostAsJsonAsync("/api/services", serviceRequest);
        var service = await serviceResponse.Content.ReadFromJsonAsync<ServiceResponse>();

        var runbookRequest = new RunbookRequest(
            Title: "Deployment Guide",
            Summary: "How to deploy"
        );

        // Act
        var response = await _client.PostAsJsonAsync($"/api/services/{service!.Id}/runbooks", runbookRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var runbook = await response.Content.ReadFromJsonAsync<RunbookResponse>();
        runbook.Should().NotBeNull();
        runbook!.Title.Should().Be("Deployment Guide");
        runbook.Status.Should().Be("draft");
        runbook.Sections.Should().HaveCount(6); // Auto-seeded sections
        runbook.Sections.Should().Contain(s => s.Key == "overview");
        runbook.Sections.Should().Contain(s => s.Key == "deploy");
        runbook.Sections.Should().Contain(s => s.Key == "rollback");
        runbook.Sections.Should().Contain(s => s.Key == "health_checks");
        runbook.Sections.Should().Contain(s => s.Key == "env_vars");
        runbook.Sections.Should().Contain(s => s.Key == "troubleshooting");
    }

    [Fact]
    public async Task GetRunbook_ShouldIncludeSections()
    {
        // Arrange - Create service and runbook
        var serviceRequest = new ServiceRequest(
            Name: "Test Service",
            Slug: "test-service",
            Description: null,
            Repo: null
        );
        var serviceResponse = await _client.PostAsJsonAsync("/api/services", serviceRequest);
        var service = await serviceResponse.Content.ReadFromJsonAsync<ServiceResponse>();

        var runbookRequest = new RunbookRequest(Title: "Test Runbook", Summary: null);
        var createResponse = await _client.PostAsJsonAsync($"/api/services/{service!.Id}/runbooks", runbookRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<RunbookResponse>();

        // Act
        var response = await _client.GetAsync($"/api/runbooks/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var runbook = await response.Content.ReadFromJsonAsync<RunbookResponse>();
        runbook.Should().NotBeNull();
        runbook!.Sections.Should().HaveCount(6);
    }

    [Fact]
    public async Task UpdateSection_ShouldPersistChanges()
    {
        // Arrange - Create service, runbook
        var serviceRequest = new ServiceRequest(
            Name: "Test Service",
            Slug: "test-service",
            Description: null,
            Repo: null
        );
        var serviceResponse = await _client.PostAsJsonAsync("/api/services", serviceRequest);
        var service = await serviceResponse.Content.ReadFromJsonAsync<ServiceResponse>();

        var runbookRequest = new RunbookRequest(Title: "Test Runbook", Summary: null);
        var createResponse = await _client.PostAsJsonAsync($"/api/services/{service!.Id}/runbooks", runbookRequest);
        var runbook = await createResponse.Content.ReadFromJsonAsync<RunbookResponse>();

        var overviewSection = runbook!.Sections.First(s => s.Key == "overview");
        var updatedMarkdown = "# Updated Overview\n\nThis is the new content.";

        var sectionRequest = new SectionRequest(
            Key: overviewSection.Key,
            Title: overviewSection.Title,
            Order: overviewSection.Order,
            BodyMarkdown: updatedMarkdown
        );

        // Act
        var updateResponse = await _client.PatchAsJsonAsync(
            $"/api/runbooks/{runbook.Id}/sections/{overviewSection.Id}",
            sectionRequest
        );

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify persistence by re-fetching
        var getResponse = await _client.GetAsync($"/api/runbooks/{runbook.Id}");
        var refetched = await getResponse.Content.ReadFromJsonAsync<RunbookResponse>();
        var refetchedSection = refetched!.Sections.First(s => s.Id == overviewSection.Id);
        refetchedSection.BodyMarkdown.Should().Be(updatedMarkdown);
    }

    [Fact]
    public async Task EndToEndFlow_ServiceToRunbookToSectionEdit_ShouldWork()
    {
        // This test covers the Week 1 acceptance criteria flow

        // 1. Create Service
        var service = await CreateTestService("My App", "my-app");
        service.Should().NotBeNull();

        // 2. Create Runbook (auto-seeds sections)
        var runbook = await CreateTestRunbook(service.Id, "Deployment Guide");
        runbook.Should().NotBeNull();
        runbook.Sections.Should().HaveCount(6);

        // 3. Edit a section
        var section = runbook.Sections.First(s => s.Key == "overview");
        var newContent = "# My Deployment\n\nFollow these steps...";
        var updated = await UpdateSection(runbook.Id, section.Id, section, newContent);
        updated.BodyMarkdown.Should().Be(newContent);

        // 4. Refresh (re-fetch) - content should persist
        var refreshed = await GetRunbook(runbook.Id);
        var refreshedSection = refreshed.Sections.First(s => s.Id == section.Id);
        refreshedSection.BodyMarkdown.Should().Be(newContent);
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

    private async Task<RunbookResponse> GetRunbook(Guid runbookId)
    {
        var response = await _client.GetAsync($"/api/runbooks/{runbookId}");
        return (await response.Content.ReadFromJsonAsync<RunbookResponse>())!;
    }

    private async Task<SectionResponse> UpdateSection(Guid runbookId, Guid sectionId, SectionResponse original, string newMarkdown)
    {
        var request = new SectionRequest(original.Key, original.Title, original.Order, newMarkdown);
        var response = await _client.PatchAsJsonAsync($"/api/runbooks/{runbookId}/sections/{sectionId}", request);
        return (await response.Content.ReadFromJsonAsync<SectionResponse>())!;
    }
}
