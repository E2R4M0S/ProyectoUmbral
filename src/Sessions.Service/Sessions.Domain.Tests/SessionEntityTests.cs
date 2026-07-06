using FluentAssertions;
using Sessions.Domain.Entities;
using Sessions.Domain.Enums;
using Xunit;

namespace Sessions.Domain.Tests;

public class SessionEntityTests
{
    [Fact]
    public void Create_ShouldInitializeCorrectly()
    {
        var session = Session.Create("Test", "123456", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Mission", "Trivia", 1, Guid.NewGuid().ToString("N")) });

        session.Name.Should().Be("Test");
        session.Pin.Should().Be("123456");
        session.Status.Should().Be(SessionStatus.Scheduled);
        session.Participants.Should().BeEmpty();
    }

    [Fact]
    public void TransitionTo_ValidTransition_ShouldSucceed()
    {
        var session = Session.Create("Test", "123456", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Mission", "Trivia", 1, Guid.NewGuid().ToString("N")) });
        session.TransitionTo(SessionStatus.Preparing);
        session.Status.Should().Be(SessionStatus.Preparing);
    }

    [Fact]
    public void TransitionTo_SameStatus_ShouldThrow()
    {
        var session = Session.Create("Test", "123456", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Mission", "Trivia", 1, Guid.NewGuid().ToString("N")) });
        var act = () => session.TransitionTo(SessionStatus.Scheduled);
        act.Should().Throw<InvalidOperationException>().WithMessage("*already in*");
    }

    [Fact]
    public void AddParticipant_WhenPreparing_ShouldAdd()
    {
        var session = Session.Create("Test", "123456", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Mission", "Trivia", 1, Guid.NewGuid().ToString("N")) });
        session.TransitionTo(SessionStatus.Preparing);
        var userId = Guid.NewGuid();
        session.AddParticipant(userId);
        session.Participants.Should().HaveCount(1);
        session.Participants[0].UserId.Should().Be(userId);
    }

    [Fact]
    public void AddParticipant_WhenScheduled_ShouldThrow()
    {
        var session = Session.Create("Test", "123456", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Mission", "Trivia", 1, Guid.NewGuid().ToString("N")) });
        var act = () => session.AddParticipant(Guid.NewGuid());
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddParticipant_Duplicate_ShouldThrow()
    {
        var session = Session.Create("Test", "123456", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Mission", "Trivia", 1, Guid.NewGuid().ToString("N")) });
        session.TransitionTo(SessionStatus.Preparing);
        var userId = Guid.NewGuid();
        session.AddParticipant(userId);
        var act = () => session.AddParticipant(userId);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void SessionParticipant_Create_ShouldSetProperties()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var participant = SessionParticipant.Create(sessionId, userId);

        participant.SessionId.Should().Be(sessionId);
        participant.UserId.Should().Be(userId);
        participant.JoinedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void SessionParticipant_CreateWithAlias_ShouldSetAlias()
    {
        var participant = SessionParticipant.Create(Guid.NewGuid(), Guid.NewGuid(), "erosd");
        participant.UserAlias.Should().Be("erosd");
    }
}
