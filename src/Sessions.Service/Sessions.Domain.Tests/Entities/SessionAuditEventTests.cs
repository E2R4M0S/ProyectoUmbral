using FluentAssertions;
using Sessions.Domain.Entities;
using Xunit;

namespace Sessions.Domain.Tests.Entities;

public class SessionAuditEventTests
{
    [Fact]
    public void Create_WithValidInputs_ShouldSetAllProperties()
    {
        var sessionId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var evt = SessionAuditEvent.Create(
            sessionId,
            SessionAuditEventTypes.PenaltyApplied,
            "Pista liberada antes de tiempo",
            teamId,
            userId,
            -25);

        evt.SessionId.Should().Be(sessionId);
        evt.EventType.Should().Be(SessionAuditEventTypes.PenaltyApplied);
        evt.Description.Should().Be("Pista liberada antes de tiempo");
        evt.TeamId.Should().Be(teamId);
        evt.UserId.Should().Be(userId);
        evt.ScoreDelta.Should().Be(-25);
        evt.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        evt.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_WithEmptyEventType_ShouldThrow()
    {
        Action act = () => SessionAuditEvent.Create(Guid.NewGuid(), "", "Description");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*EventType*");
    }

    [Fact]
    public void Create_WithEmptyDescription_ShouldThrow()
    {
        Action act = () => SessionAuditEvent.Create(Guid.NewGuid(), SessionAuditEventTypes.StatusChanged, "");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Description*");
    }

    [Fact]
    public void Create_ShouldTrimDescriptionWhitespace()
    {
        var evt = SessionAuditEvent.Create(Guid.NewGuid(), SessionAuditEventTypes.StatusChanged, "  Sesión activada  ");

        evt.Description.Should().Be("Sesión activada");
    }

    [Fact]
    public void Create_WithoutOptionalFields_ShouldLeaveThemNull()
    {
        var evt = SessionAuditEvent.Create(Guid.NewGuid(), SessionAuditEventTypes.StatusChanged, "Sesión activada");

        evt.TeamId.Should().BeNull();
        evt.UserId.Should().BeNull();
        evt.ScoreDelta.Should().BeNull();
    }
}
