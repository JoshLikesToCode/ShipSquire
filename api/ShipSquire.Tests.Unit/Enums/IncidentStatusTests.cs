using FluentAssertions;
using ShipSquire.Domain.Enums;
using Xunit;

namespace ShipSquire.Tests.Unit.Enums;

public class IncidentStatusTests
{
    [Theory]
    [InlineData(IncidentStatus.Open, IncidentStatus.Investigating, true)]
    [InlineData(IncidentStatus.Investigating, IncidentStatus.Mitigated, true)]
    [InlineData(IncidentStatus.Investigating, IncidentStatus.Resolved, true)]
    [InlineData(IncidentStatus.Mitigated, IncidentStatus.Resolved, true)]
    [InlineData(IncidentStatus.Mitigated, IncidentStatus.Investigating, true)]
    [InlineData(IncidentStatus.Resolved, IncidentStatus.Open, true)]
    public void CanTransition_ValidTransitions_ReturnsTrue(string from, string to, bool expected)
    {
        IncidentStatus.CanTransition(from, to).Should().Be(expected);
    }

    [Theory]
    [InlineData(IncidentStatus.Open, IncidentStatus.Mitigated)]
    [InlineData(IncidentStatus.Open, IncidentStatus.Resolved)]
    [InlineData(IncidentStatus.Investigating, IncidentStatus.Open)]
    [InlineData(IncidentStatus.Mitigated, IncidentStatus.Open)]
    [InlineData(IncidentStatus.Resolved, IncidentStatus.Investigating)]
    [InlineData(IncidentStatus.Resolved, IncidentStatus.Mitigated)]
    public void CanTransition_InvalidTransitions_ReturnsFalse(string from, string to)
    {
        IncidentStatus.CanTransition(from, to).Should().BeFalse();
    }

    [Fact]
    public void CanTransition_SameStatus_ReturnsFalse()
    {
        IncidentStatus.CanTransition(IncidentStatus.Open, IncidentStatus.Open).Should().BeFalse();
        IncidentStatus.CanTransition(IncidentStatus.Investigating, IncidentStatus.Investigating).Should().BeFalse();
        IncidentStatus.CanTransition(IncidentStatus.Mitigated, IncidentStatus.Mitigated).Should().BeFalse();
        IncidentStatus.CanTransition(IncidentStatus.Resolved, IncidentStatus.Resolved).Should().BeFalse();
    }

    [Fact]
    public void CanTransition_InvalidStatus_ReturnsFalse()
    {
        IncidentStatus.CanTransition("invalid", IncidentStatus.Open).Should().BeFalse();
        IncidentStatus.CanTransition(IncidentStatus.Open, "invalid").Should().BeFalse();
    }

    [Fact]
    public void GetValidTransitions_FromOpen_ReturnsInvestigatingOnly()
    {
        var transitions = IncidentStatus.GetValidTransitions(IncidentStatus.Open);
        transitions.Should().BeEquivalentTo(new[] { IncidentStatus.Investigating });
    }

    [Fact]
    public void GetValidTransitions_FromInvestigating_ReturnsMitigatedAndResolved()
    {
        var transitions = IncidentStatus.GetValidTransitions(IncidentStatus.Investigating);
        transitions.Should().BeEquivalentTo(new[] { IncidentStatus.Mitigated, IncidentStatus.Resolved });
    }

    [Fact]
    public void GetValidTransitions_FromMitigated_ReturnsResolvedAndInvestigating()
    {
        var transitions = IncidentStatus.GetValidTransitions(IncidentStatus.Mitigated);
        transitions.Should().BeEquivalentTo(new[] { IncidentStatus.Resolved, IncidentStatus.Investigating });
    }

    [Fact]
    public void GetValidTransitions_FromResolved_ReturnsOpenOnly()
    {
        var transitions = IncidentStatus.GetValidTransitions(IncidentStatus.Resolved);
        transitions.Should().BeEquivalentTo(new[] { IncidentStatus.Open });
    }

    [Fact]
    public void GetValidTransitions_InvalidStatus_ReturnsEmpty()
    {
        var transitions = IncidentStatus.GetValidTransitions("invalid");
        transitions.Should().BeEmpty();
    }
}
