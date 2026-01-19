using FluentAssertions;
using Moq;
using ShipSquire.Application.DTOs;
using ShipSquire.Application.Interfaces;
using ShipSquire.Application.Services;
using ShipSquire.Domain.Entities;
using ShipSquire.Domain.Enums;
using ShipSquire.Domain.Interfaces;
using Xunit;

namespace ShipSquire.Tests.Unit.Services;

public class TimelineEntryServiceTests
{
    private readonly Mock<ITimelineEntryRepository> _timelineRepoMock;
    private readonly Mock<IIncidentRepository> _incidentRepoMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly TimelineEntryService _service;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _incidentId = Guid.NewGuid();

    public TimelineEntryServiceTests()
    {
        _timelineRepoMock = new Mock<ITimelineEntryRepository>();
        _incidentRepoMock = new Mock<IIncidentRepository>();
        _currentUserMock = new Mock<ICurrentUser>();

        _currentUserMock.Setup(u => u.UserId).Returns(_userId);

        _service = new TimelineEntryService(
            _timelineRepoMock.Object,
            _incidentRepoMock.Object,
            _currentUserMock.Object
        );
    }

    [Fact]
    public async Task AddEntryAsync_WithValidRequest_CreatesEntryWithServerTimestamp()
    {
        // Arrange
        var incident = new Incident { Id = _incidentId, UserId = _userId };
        _incidentRepoMock
            .Setup(r => r.GetByIdAndUserIdAsync(_incidentId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        IncidentTimelineEntry? capturedEntry = null;
        _timelineRepoMock
            .Setup(r => r.AddAsync(It.IsAny<IncidentTimelineEntry>(), It.IsAny<CancellationToken>()))
            .Callback<IncidentTimelineEntry, CancellationToken>((e, _) => capturedEntry = e)
            .ReturnsAsync((IncidentTimelineEntry e, CancellationToken _) => e);

        var beforeTime = DateTimeOffset.UtcNow;
        var request = new TimelineEntryRequest(TimelineEntryType.Note, "This is a note");

        // Act
        var result = await _service.AddEntryAsync(_incidentId, request);

        // Assert
        result.Should().NotBeNull();
        result!.EntryType.Should().Be(TimelineEntryType.Note);
        result.BodyMarkdown.Should().Be("This is a note");
        result.IncidentId.Should().Be(_incidentId);

        // Verify server-side timestamp was set
        capturedEntry.Should().NotBeNull();
        capturedEntry!.OccurredAt.Should().BeOnOrAfter(beforeTime);
        capturedEntry.OccurredAt.Should().BeOnOrBefore(DateTimeOffset.UtcNow);
    }

    [Theory]
    [InlineData(TimelineEntryType.Note)]
    [InlineData(TimelineEntryType.Action)]
    [InlineData(TimelineEntryType.Decision)]
    [InlineData(TimelineEntryType.Observation)]
    public async Task AddEntryAsync_WithValidEntryTypes_AcceptsAll(string entryType)
    {
        // Arrange
        var incident = new Incident { Id = _incidentId, UserId = _userId };
        _incidentRepoMock
            .Setup(r => r.GetByIdAndUserIdAsync(_incidentId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        _timelineRepoMock
            .Setup(r => r.AddAsync(It.IsAny<IncidentTimelineEntry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IncidentTimelineEntry e, CancellationToken _) => e);

        var request = new TimelineEntryRequest(entryType, "Test body");

        // Act
        var result = await _service.AddEntryAsync(_incidentId, request);

        // Assert
        result.Should().NotBeNull();
        result!.EntryType.Should().Be(entryType);
    }

    [Fact]
    public async Task AddEntryAsync_WithInvalidEntryType_ThrowsArgumentException()
    {
        // Arrange
        var incident = new Incident { Id = _incidentId, UserId = _userId };
        _incidentRepoMock
            .Setup(r => r.GetByIdAndUserIdAsync(_incidentId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        var request = new TimelineEntryRequest("invalid_type", "Test body");

        // Act & Assert
        var action = () => _service.AddEntryAsync(_incidentId, request);
        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Invalid entry type*");
    }

    [Fact]
    public async Task AddEntryAsync_WithEmptyBody_ThrowsArgumentException()
    {
        // Arrange
        var incident = new Incident { Id = _incidentId, UserId = _userId };
        _incidentRepoMock
            .Setup(r => r.GetByIdAndUserIdAsync(_incidentId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        var request = new TimelineEntryRequest(TimelineEntryType.Note, "");

        // Act & Assert
        var action = () => _service.AddEntryAsync(_incidentId, request);
        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Body markdown is required*");
    }

    [Fact]
    public async Task AddEntryAsync_WithWhitespaceOnlyBody_ThrowsArgumentException()
    {
        // Arrange
        var incident = new Incident { Id = _incidentId, UserId = _userId };
        _incidentRepoMock
            .Setup(r => r.GetByIdAndUserIdAsync(_incidentId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        var request = new TimelineEntryRequest(TimelineEntryType.Action, "   ");

        // Act & Assert
        var action = () => _service.AddEntryAsync(_incidentId, request);
        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Body markdown is required*");
    }

    [Fact]
    public async Task AddEntryAsync_WithNonOwnedIncident_ReturnsNull()
    {
        // Arrange
        _incidentRepoMock
            .Setup(r => r.GetByIdAndUserIdAsync(_incidentId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Incident?)null);

        var request = new TimelineEntryRequest(TimelineEntryType.Note, "Test body");

        // Act
        var result = await _service.AddEntryAsync(_incidentId, request);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIncidentIdAsync_ReturnsEntriesInOrder()
    {
        // Arrange
        var incident = new Incident { Id = _incidentId, UserId = _userId };
        _incidentRepoMock
            .Setup(r => r.GetByIdAndUserIdAsync(_incidentId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        var entries = new List<IncidentTimelineEntry>
        {
            new() { Id = Guid.NewGuid(), IncidentId = _incidentId, EntryType = TimelineEntryType.Note, OccurredAt = DateTimeOffset.UtcNow.AddMinutes(-30), BodyMarkdown = "First" },
            new() { Id = Guid.NewGuid(), IncidentId = _incidentId, EntryType = TimelineEntryType.Action, OccurredAt = DateTimeOffset.UtcNow.AddMinutes(-20), BodyMarkdown = "Second" },
            new() { Id = Guid.NewGuid(), IncidentId = _incidentId, EntryType = TimelineEntryType.Decision, OccurredAt = DateTimeOffset.UtcNow.AddMinutes(-10), BodyMarkdown = "Third" },
        };

        _timelineRepoMock
            .Setup(r => r.GetByIncidentIdAsync(_incidentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entries);

        // Act
        var result = (await _service.GetByIncidentIdAsync(_incidentId)).ToList();

        // Assert
        result.Should().HaveCount(3);
        result[0].BodyMarkdown.Should().Be("First");
        result[1].BodyMarkdown.Should().Be("Second");
        result[2].BodyMarkdown.Should().Be("Third");
    }

    [Fact]
    public async Task GetByIncidentIdAsync_WithNonOwnedIncident_ReturnsEmpty()
    {
        // Arrange
        _incidentRepoMock
            .Setup(r => r.GetByIdAndUserIdAsync(_incidentId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Incident?)null);

        // Act
        var result = await _service.GetByIncidentIdAsync(_incidentId);

        // Assert
        result.Should().BeEmpty();
    }
}
