using FluentAssertions;
using Moq;
using ShipSquire.Application.DTOs;
using ShipSquire.Application.Exceptions;
using ShipSquire.Application.Interfaces;
using ShipSquire.Application.Services;
using ShipSquire.Domain.Entities;
using ShipSquire.Domain.Enums;
using ShipSquire.Domain.Interfaces;
using Xunit;

namespace ShipSquire.Tests.Unit.Services;

public class IncidentServiceTests
{
    private readonly Mock<IIncidentRepository> _incidentRepoMock;
    private readonly Mock<IServiceRepository> _serviceRepoMock;
    private readonly Mock<IRunbookRepository> _runbookRepoMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly IncidentService _service;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _serviceId = Guid.NewGuid();

    public IncidentServiceTests()
    {
        _incidentRepoMock = new Mock<IIncidentRepository>();
        _serviceRepoMock = new Mock<IServiceRepository>();
        _runbookRepoMock = new Mock<IRunbookRepository>();
        _currentUserMock = new Mock<ICurrentUser>();

        _currentUserMock.Setup(u => u.UserId).Returns(_userId);

        _service = new IncidentService(
            _incidentRepoMock.Object,
            _serviceRepoMock.Object,
            _runbookRepoMock.Object,
            _currentUserMock.Object
        );
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesIncidentWithOpenStatus()
    {
        // Arrange
        var mockService = new Service { Id = _serviceId, UserId = _userId, Name = "Test Service" };
        _serviceRepoMock
            .Setup(r => r.GetByIdAndUserIdAsync(_serviceId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockService);

        _runbookRepoMock
            .Setup(r => r.GetLatestForServiceAsync(_serviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Runbook?)null);

        Incident? capturedIncident = null;
        _incidentRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Incident>(), It.IsAny<CancellationToken>()))
            .Callback<Incident, CancellationToken>((i, _) => capturedIncident = i)
            .ReturnsAsync((Incident i, CancellationToken _) => i);

        _incidentRepoMock
            .Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => capturedIncident);

        var request = new IncidentRequest("Test Incident", IncidentSeverity.Sev2, DateTimeOffset.UtcNow);

        // Act
        var result = await _service.CreateAsync(_serviceId, request);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(IncidentStatus.Open);
        result.Severity.Should().Be(IncidentSeverity.Sev2);
        result.Title.Should().Be("Test Incident");
    }

    [Fact]
    public async Task CreateAsync_WithPublishedRunbook_AttachesRunbook()
    {
        // Arrange
        var runbookId = Guid.NewGuid();
        var mockService = new Service { Id = _serviceId, UserId = _userId, Name = "Test Service" };
        var mockRunbook = new Runbook
        {
            Id = runbookId,
            ServiceId = _serviceId,
            Title = "Published Runbook",
            Status = "published"
        };

        _serviceRepoMock
            .Setup(r => r.GetByIdAndUserIdAsync(_serviceId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockService);

        _runbookRepoMock
            .Setup(r => r.GetLatestForServiceAsync(_serviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockRunbook);

        Incident? capturedIncident = null;
        _incidentRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Incident>(), It.IsAny<CancellationToken>()))
            .Callback<Incident, CancellationToken>((i, _) =>
            {
                capturedIncident = i;
                capturedIncident.Runbook = mockRunbook;
            })
            .ReturnsAsync((Incident i, CancellationToken _) => i);

        _incidentRepoMock
            .Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => capturedIncident);

        var request = new IncidentRequest("Test Incident", IncidentSeverity.Sev1, DateTimeOffset.UtcNow);

        // Act
        var result = await _service.CreateAsync(_serviceId, request);

        // Assert
        result.Should().NotBeNull();
        result!.RunbookId.Should().Be(runbookId);
        result.RunbookTitle.Should().Be("Published Runbook");
    }

    [Fact]
    public async Task CreateAsync_WithInvalidSeverity_ThrowsValidationException()
    {
        // Arrange
        var mockService = new Service { Id = _serviceId, UserId = _userId, Name = "Test Service" };
        _serviceRepoMock
            .Setup(r => r.GetByIdAndUserIdAsync(_serviceId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockService);

        var request = new IncidentRequest("Test Incident", "invalid_severity", DateTimeOffset.UtcNow);

        // Act & Assert
        var action = () => _service.CreateAsync(_serviceId, request);
        await action.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Invalid severity*");
    }

    [Fact]
    public async Task CreateAsync_WithNonOwnedService_ReturnsNull()
    {
        // Arrange
        _serviceRepoMock
            .Setup(r => r.GetByIdAndUserIdAsync(_serviceId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Service?)null);

        var request = new IncidentRequest("Test Incident", IncidentSeverity.Sev3, DateTimeOffset.UtcNow);

        // Act
        var result = await _service.CreateAsync(_serviceId, request);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WithNonOwnedIncident_ReturnsNull()
    {
        // Arrange
        var incidentId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var incident = new Incident
        {
            Id = incidentId,
            UserId = otherUserId,  // Different user
            ServiceId = _serviceId,
            Title = "Other's Incident"
        };

        _incidentRepoMock
            .Setup(r => r.GetByIdWithDetailsAsync(incidentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        // Act
        var result = await _service.GetByIdAsync(incidentId);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData(IncidentSeverity.Sev1)]
    [InlineData(IncidentSeverity.Sev2)]
    [InlineData(IncidentSeverity.Sev3)]
    [InlineData(IncidentSeverity.Sev4)]
    public async Task CreateAsync_WithValidSeverities_AcceptsAll(string severity)
    {
        // Arrange
        var mockService = new Service { Id = _serviceId, UserId = _userId, Name = "Test Service" };
        _serviceRepoMock
            .Setup(r => r.GetByIdAndUserIdAsync(_serviceId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockService);

        _runbookRepoMock
            .Setup(r => r.GetLatestForServiceAsync(_serviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Runbook?)null);

        Incident? capturedIncident = null;
        _incidentRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Incident>(), It.IsAny<CancellationToken>()))
            .Callback<Incident, CancellationToken>((i, _) => capturedIncident = i)
            .ReturnsAsync((Incident i, CancellationToken _) => i);

        _incidentRepoMock
            .Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => capturedIncident);

        var request = new IncidentRequest("Test Incident", severity, DateTimeOffset.UtcNow);

        // Act
        var result = await _service.CreateAsync(_serviceId, request);

        // Assert
        result.Should().NotBeNull();
        result!.Severity.Should().Be(severity);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidStatusTransition_ThrowsInvalidStatusTransitionException()
    {
        // Arrange
        var incidentId = Guid.NewGuid();
        var incident = new Incident
        {
            Id = incidentId,
            UserId = _userId,
            ServiceId = _serviceId,
            Title = "Test Incident",
            Status = IncidentStatus.Open
        };

        _incidentRepoMock
            .Setup(r => r.GetByIdAndUserIdAsync(incidentId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        // Open cannot go directly to Resolved
        var request = new IncidentUpdateRequest(Status: IncidentStatus.Resolved);

        // Act & Assert
        var action = () => _service.UpdateAsync(incidentId, request);
        await action.Should().ThrowAsync<InvalidStatusTransitionException>()
            .WithMessage("*Cannot change status*");
    }

    [Fact]
    public async Task TransitionStatusAsync_ValidTransition_ReturnsSuccessResponse()
    {
        // Arrange
        var incidentId = Guid.NewGuid();
        var incident = new Incident
        {
            Id = incidentId,
            UserId = _userId,
            ServiceId = _serviceId,
            Title = "Test Incident",
            Status = IncidentStatus.Open
        };

        _incidentRepoMock
            .Setup(r => r.GetByIdAndUserIdAsync(incidentId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        _incidentRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Incident>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var request = new StatusTransitionRequest(IncidentStatus.Investigating);

        // Act
        var result = await _service.TransitionStatusAsync(incidentId, request);

        // Assert
        result.Should().NotBeNull();
        result!.PreviousStatus.Should().Be(IncidentStatus.Open);
        result.NewStatus.Should().Be(IncidentStatus.Investigating);
    }

    [Fact]
    public async Task TransitionStatusAsync_InvalidTransition_ThrowsInvalidStatusTransitionException()
    {
        // Arrange
        var incidentId = Guid.NewGuid();
        var incident = new Incident
        {
            Id = incidentId,
            UserId = _userId,
            ServiceId = _serviceId,
            Title = "Test Incident",
            Status = IncidentStatus.Open
        };

        _incidentRepoMock
            .Setup(r => r.GetByIdAndUserIdAsync(incidentId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        // Open cannot go directly to Mitigated
        var request = new StatusTransitionRequest(IncidentStatus.Mitigated);

        // Act & Assert
        var action = () => _service.TransitionStatusAsync(incidentId, request);
        await action.Should().ThrowAsync<InvalidStatusTransitionException>()
            .WithMessage("*Cannot change status*");
    }

    [Fact]
    public async Task TransitionStatusAsync_ToResolved_SetsEndedAtAutomatically()
    {
        // Arrange
        var incidentId = Guid.NewGuid();
        var incident = new Incident
        {
            Id = incidentId,
            UserId = _userId,
            ServiceId = _serviceId,
            Title = "Test Incident",
            Status = IncidentStatus.Investigating,
            StartedAt = DateTimeOffset.UtcNow.AddHours(-1),
            EndedAt = null
        };

        _incidentRepoMock
            .Setup(r => r.GetByIdAndUserIdAsync(incidentId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        Incident? capturedIncident = null;
        _incidentRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Incident>(), It.IsAny<CancellationToken>()))
            .Callback<Incident, CancellationToken>((i, _) => capturedIncident = i)
            .Returns(Task.CompletedTask);

        var request = new StatusTransitionRequest(IncidentStatus.Resolved);

        // Act
        var result = await _service.TransitionStatusAsync(incidentId, request);

        // Assert
        result.Should().NotBeNull();
        result!.NewStatus.Should().Be(IncidentStatus.Resolved);
        result.EndedAt.Should().NotBeNull();
        capturedIncident!.EndedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task TransitionStatusAsync_Reopen_ClearsEndedAt()
    {
        // Arrange
        var incidentId = Guid.NewGuid();
        var incident = new Incident
        {
            Id = incidentId,
            UserId = _userId,
            ServiceId = _serviceId,
            Title = "Test Incident",
            Status = IncidentStatus.Resolved,
            StartedAt = DateTimeOffset.UtcNow.AddHours(-2),
            EndedAt = DateTimeOffset.UtcNow.AddHours(-1)
        };

        _incidentRepoMock
            .Setup(r => r.GetByIdAndUserIdAsync(incidentId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        Incident? capturedIncident = null;
        _incidentRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Incident>(), It.IsAny<CancellationToken>()))
            .Callback<Incident, CancellationToken>((i, _) => capturedIncident = i)
            .Returns(Task.CompletedTask);

        var request = new StatusTransitionRequest(IncidentStatus.Open);

        // Act
        var result = await _service.TransitionStatusAsync(incidentId, request);

        // Assert
        result.Should().NotBeNull();
        result!.NewStatus.Should().Be(IncidentStatus.Open);
        result.EndedAt.Should().BeNull();
        capturedIncident!.EndedAt.Should().BeNull();
    }

    [Fact]
    public async Task TransitionStatusAsync_WithNonOwnedIncident_ReturnsNull()
    {
        // Arrange
        var incidentId = Guid.NewGuid();

        _incidentRepoMock
            .Setup(r => r.GetByIdAndUserIdAsync(incidentId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Incident?)null);

        var request = new StatusTransitionRequest(IncidentStatus.Investigating);

        // Act
        var result = await _service.TransitionStatusAsync(incidentId, request);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task TransitionStatusAsync_WithInvalidStatus_ThrowsValidationException()
    {
        // Arrange
        var incidentId = Guid.NewGuid();
        var incident = new Incident
        {
            Id = incidentId,
            UserId = _userId,
            ServiceId = _serviceId,
            Title = "Test Incident",
            Status = IncidentStatus.Open
        };

        _incidentRepoMock
            .Setup(r => r.GetByIdAndUserIdAsync(incidentId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        var request = new StatusTransitionRequest("invalid_status");

        // Act & Assert
        var action = () => _service.TransitionStatusAsync(incidentId, request);
        await action.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Invalid status*");
    }
}
