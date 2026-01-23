using FluentAssertions;
using Moq;
using ShipSquire.Application.Interfaces;
using ShipSquire.Application.Services;
using ShipSquire.Domain.Entities;
using ShipSquire.Domain.Enums;
using ShipSquire.Domain.Interfaces;
using Xunit;

namespace ShipSquire.Tests.Unit.Services;

public class MarkdownExportServiceTests
{
    private readonly Mock<IIncidentRepository> _incidentRepoMock;
    private readonly Mock<ITimelineEntryRepository> _timelineRepoMock;
    private readonly Mock<IPostmortemRepository> _postmortemRepoMock;
    private readonly Mock<IServiceRepository> _serviceRepoMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly MarkdownExportService _service;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _serviceId = Guid.NewGuid();
    private readonly Guid _incidentId = Guid.NewGuid();

    public MarkdownExportServiceTests()
    {
        _incidentRepoMock = new Mock<IIncidentRepository>();
        _timelineRepoMock = new Mock<ITimelineEntryRepository>();
        _postmortemRepoMock = new Mock<IPostmortemRepository>();
        _serviceRepoMock = new Mock<IServiceRepository>();
        _currentUserMock = new Mock<ICurrentUser>();

        _currentUserMock.Setup(u => u.UserId).Returns(_userId);

        _service = new MarkdownExportService(
            _incidentRepoMock.Object,
            _timelineRepoMock.Object,
            _postmortemRepoMock.Object,
            _serviceRepoMock.Object,
            _currentUserMock.Object
        );
    }

    [Fact]
    public async Task ExportIncidentAsync_BasicIncident_ContainsExpectedSections()
    {
        // Arrange
        var startedAt = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
        var incident = CreateIncident(startedAt);
        var service = CreateService();

        SetupMocks(incident, service, new List<IncidentTimelineEntry>(), null);

        // Act
        var result = await _service.ExportIncidentAsync(_incidentId);

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().Contain("# Incident Report: API Outage");
        result.Content.Should().Contain("## Overview");
        result.Content.Should().Contain("| **Service** | Test Service |");
        result.Content.Should().Contain("| **Severity** | SEV2 |");
        result.Content.Should().Contain("| **Status** | Open |");
        result.Content.Should().Contain("| **Started** | 2024-01-15 10:30:00 UTC |");
        result.Content.Should().Contain("## Timeline");
        result.Content.Should().Contain("*No timeline entries recorded.*");
        result.Content.Should().Contain("*Exported from ShipSquire on");
        result.Filename.Should().Be("incident-2024-01-15-api-outage.md");
        result.ContentType.Should().Be("text/markdown");
    }

    [Fact]
    public async Task ExportIncidentAsync_WithTimeline_ContainsTimelineEntries()
    {
        // Arrange
        var startedAt = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
        var incident = CreateIncident(startedAt);
        var service = CreateService();
        var timeline = new List<IncidentTimelineEntry>
        {
            new() {
                Id = Guid.NewGuid(),
                IncidentId = _incidentId,
                EntryType = TimelineEntryType.Note,
                OccurredAt = startedAt.AddMinutes(5),
                BodyMarkdown = "Started investigating the issue"
            },
            new() {
                Id = Guid.NewGuid(),
                IncidentId = _incidentId,
                EntryType = TimelineEntryType.Action,
                OccurredAt = startedAt.AddMinutes(15),
                BodyMarkdown = "Restarted the service"
            },
            new() {
                Id = Guid.NewGuid(),
                IncidentId = _incidentId,
                EntryType = TimelineEntryType.Decision,
                OccurredAt = startedAt.AddMinutes(30),
                BodyMarkdown = "Decided to rollback"
            },
            new() {
                Id = Guid.NewGuid(),
                IncidentId = _incidentId,
                EntryType = TimelineEntryType.Observation,
                OccurredAt = startedAt.AddMinutes(45),
                BodyMarkdown = "System recovered"
            }
        };

        SetupMocks(incident, service, timeline, null);

        // Act
        var result = await _service.ExportIncidentAsync(_incidentId);

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().Contain("## Timeline");
        result.Content.Should().Contain("📝 Note");
        result.Content.Should().Contain("Started investigating the issue");
        result.Content.Should().Contain("⚡ Action");
        result.Content.Should().Contain("Restarted the service");
        result.Content.Should().Contain("🎯 Decision");
        result.Content.Should().Contain("Decided to rollback");
        result.Content.Should().Contain("👁️ Observation");
        result.Content.Should().Contain("System recovered");
        result.Content.Should().NotContain("*No timeline entries recorded.*");
    }

    [Fact]
    public async Task ExportIncidentAsync_ResolvedWithPostmortem_ContainsPostmortemSections()
    {
        // Arrange
        var startedAt = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
        var endedAt = startedAt.AddHours(2);
        var incident = CreateIncident(startedAt, IncidentStatus.Resolved, endedAt);
        var service = CreateService();
        var postmortem = new Postmortem
        {
            Id = Guid.NewGuid(),
            IncidentId = _incidentId,
            ImpactMarkdown = "## Impact Summary\n\n100 customers affected.",
            RootCauseMarkdown = "## Root Cause Analysis\n\nDatabase connection pool exhaustion.",
            DetectionMarkdown = "## Detection\n\nAlerts triggered at 10:35.",
            ResolutionMarkdown = "## Resolution\n\nIncreased pool size.",
            ActionItemsMarkdown = "## Action Items\n\n| Action | Owner |\n|--------|-------|\n| Add monitoring | Team |"
        };

        SetupMocks(incident, service, new List<IncidentTimelineEntry>(), postmortem);

        // Act
        var result = await _service.ExportIncidentAsync(_incidentId);

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().Contain("| **Status** | Resolved |");
        result.Content.Should().Contain("| **Ended** | 2024-01-15 12:30:00 UTC |");
        result.Content.Should().Contain("| **Duration** | 2h 0m |");
        result.Content.Should().Contain("# Postmortem");
        result.Content.Should().Contain("100 customers affected.");
        result.Content.Should().Contain("Database connection pool exhaustion.");
        result.Content.Should().Contain("Alerts triggered at 10:35.");
        result.Content.Should().Contain("Increased pool size.");
        result.Content.Should().Contain("Add monitoring");
    }

    [Fact]
    public async Task ExportIncidentAsync_WithSummary_IncludesSummarySection()
    {
        // Arrange
        var startedAt = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
        var incident = CreateIncident(startedAt);
        incident.SummaryMarkdown = "Critical API outage affecting production users.";
        var service = CreateService();

        SetupMocks(incident, service, new List<IncidentTimelineEntry>(), null);

        // Act
        var result = await _service.ExportIncidentAsync(_incidentId);

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().Contain("## Summary");
        result.Content.Should().Contain("Critical API outage affecting production users.");
    }

    [Fact]
    public async Task ExportIncidentAsync_WithRunbook_IncludesRunbookReference()
    {
        // Arrange
        var startedAt = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
        var runbookId = Guid.NewGuid();
        var incident = CreateIncident(startedAt);
        incident.RunbookId = runbookId;
        incident.Runbook = new Runbook
        {
            Id = runbookId,
            ServiceId = _serviceId,
            Title = "API Recovery Runbook"
        };
        var service = CreateService();

        SetupMocks(incident, service, new List<IncidentTimelineEntry>(), null);

        // Act
        var result = await _service.ExportIncidentAsync(_incidentId);

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().Contain("| **Runbook** | API Recovery Runbook |");
    }

    [Fact]
    public async Task ExportIncidentAsync_SanitizesSecrets_RedactsApiKeys()
    {
        // Arrange
        var startedAt = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
        var incident = CreateIncident(startedAt);
        incident.SummaryMarkdown = "API issue with api_key=abc123secret and token: xyz789token";
        var service = CreateService();
        var timeline = new List<IncidentTimelineEntry>
        {
            new() {
                Id = Guid.NewGuid(),
                IncidentId = _incidentId,
                EntryType = TimelineEntryType.Note,
                OccurredAt = startedAt.AddMinutes(5),
                BodyMarkdown = "Found password=supersecret in logs"
            }
        };

        SetupMocks(incident, service, timeline, null);

        // Act
        var result = await _service.ExportIncidentAsync(_incidentId);

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().Contain("api_key=[REDACTED]");
        result.Content.Should().Contain("token=[REDACTED]");
        result.Content.Should().Contain("password=[REDACTED]");
        result.Content.Should().NotContain("abc123secret");
        result.Content.Should().NotContain("xyz789token");
        result.Content.Should().NotContain("supersecret");
    }

    [Fact]
    public async Task ExportIncidentAsync_SanitizesSecrets_RedactsAwsKeys()
    {
        // Arrange
        var startedAt = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
        var incident = CreateIncident(startedAt);
        incident.SummaryMarkdown = "AWS error with key AKIAIOSFODNN7EXAMPLE";
        var service = CreateService();

        SetupMocks(incident, service, new List<IncidentTimelineEntry>(), null);

        // Act
        var result = await _service.ExportIncidentAsync(_incidentId);

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().Contain("[REDACTED_AWS_KEY]");
        result.Content.Should().NotContain("AKIAIOSFODNN7EXAMPLE");
    }

    [Fact]
    public async Task ExportIncidentAsync_SanitizesSecrets_RedactsJwtTokens()
    {
        // Arrange
        var startedAt = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
        var incident = CreateIncident(startedAt);
        // Use "JWT:" prefix instead of "Token:" to avoid the token pattern matching first
        incident.SummaryMarkdown = "JWT eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.dozjgNryP4J3jVmNHl0w5N_XgL0n3I9PlFUP0THsR8U";
        var service = CreateService();

        SetupMocks(incident, service, new List<IncidentTimelineEntry>(), null);

        // Act
        var result = await _service.ExportIncidentAsync(_incidentId);

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().Contain("[REDACTED_JWT]");
        result.Content.Should().NotContain("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9");
    }

    [Fact]
    public async Task ExportIncidentAsync_WithSpecialCharactersInTitle_SanitizesFilename()
    {
        // Arrange
        var startedAt = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
        var incident = CreateIncident(startedAt);
        incident.Title = "Critical Bug: API/Auth #123 (prod)";
        var service = CreateService();

        SetupMocks(incident, service, new List<IncidentTimelineEntry>(), null);

        // Act
        var result = await _service.ExportIncidentAsync(_incidentId);

        // Assert
        result.Should().NotBeNull();
        result!.Filename.Should().MatchRegex(@"^incident-2024-01-15-[\w\-]+\.md$");
        result.Filename.Should().NotContain("/");
        result.Filename.Should().NotContain("#");
        result.Filename.Should().NotContain("(");
        result.Filename.Should().NotContain(")");
    }

    [Fact]
    public async Task ExportIncidentAsync_NonOwnedIncident_ReturnsNull()
    {
        // Arrange
        var otherUserId = Guid.NewGuid();
        var incident = CreateIncident(DateTimeOffset.UtcNow);
        incident.UserId = otherUserId;

        _incidentRepoMock
            .Setup(r => r.GetByIdWithDetailsAsync(_incidentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        // Act
        var result = await _service.ExportIncidentAsync(_incidentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExportIncidentAsync_NonExistentIncident_ReturnsNull()
    {
        // Arrange
        _incidentRepoMock
            .Setup(r => r.GetByIdWithDetailsAsync(_incidentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Incident?)null);

        // Act
        var result = await _service.ExportIncidentAsync(_incidentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExportIncidentAsync_LongDuration_FormatsDurationCorrectly()
    {
        // Arrange
        var startedAt = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
        var endedAt = startedAt.AddDays(1).AddHours(5).AddMinutes(30);
        var incident = CreateIncident(startedAt, IncidentStatus.Resolved, endedAt);
        var service = CreateService();

        SetupMocks(incident, service, new List<IncidentTimelineEntry>(), null);

        // Act
        var result = await _service.ExportIncidentAsync(_incidentId);

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().Contain("| **Duration** | 1d 5h 30m |");
    }

    [Fact]
    public async Task ExportIncidentAsync_ShortDuration_FormatsDurationCorrectly()
    {
        // Arrange
        var startedAt = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
        var endedAt = startedAt.AddMinutes(45);
        var incident = CreateIncident(startedAt, IncidentStatus.Resolved, endedAt);
        var service = CreateService();

        SetupMocks(incident, service, new List<IncidentTimelineEntry>(), null);

        // Act
        var result = await _service.ExportIncidentAsync(_incidentId);

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().Contain("| **Duration** | 45m |");
    }

    private Incident CreateIncident(DateTimeOffset startedAt, string status = IncidentStatus.Open, DateTimeOffset? endedAt = null)
    {
        return new Incident
        {
            Id = _incidentId,
            UserId = _userId,
            ServiceId = _serviceId,
            Title = "API Outage",
            Severity = IncidentSeverity.Sev2,
            Status = status,
            StartedAt = startedAt,
            EndedAt = endedAt
        };
    }

    private Service CreateService()
    {
        return new Service
        {
            Id = _serviceId,
            UserId = _userId,
            Name = "Test Service",
            Slug = "test-service"
        };
    }

    private void SetupMocks(Incident incident, Service service, IEnumerable<IncidentTimelineEntry> timeline, Postmortem? postmortem)
    {
        _incidentRepoMock
            .Setup(r => r.GetByIdWithDetailsAsync(_incidentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        _serviceRepoMock
            .Setup(r => r.GetByIdAndUserIdAsync(_serviceId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);

        _timelineRepoMock
            .Setup(r => r.GetByIncidentIdAsync(_incidentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(timeline);

        _postmortemRepoMock
            .Setup(r => r.GetByIncidentIdAsync(_incidentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(postmortem);
    }
}
