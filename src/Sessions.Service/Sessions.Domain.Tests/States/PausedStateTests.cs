using System.Reflection;
using FluentAssertions;
using Sessions.Domain.Entities;
using Sessions.Domain.Enums;
using Sessions.Domain.States;
using Xunit;

namespace Sessions.Domain.Tests.States;

public class PausedStateTests
{
    private readonly PausedState _state = new();

    [Fact]
    public void Status_ShouldReturnPaused()
    {
        _state.Status.Should().Be(SessionStatus.Paused);
    }

    [Theory]
    [InlineData(SessionStatus.Active)]
    [InlineData(SessionStatus.Finished)]
    [InlineData(SessionStatus.Cancelled)]
    public void CanTransitionTo_ValidTargets_ShouldReturnTrue(SessionStatus target)
    {
        _state.CanTransitionTo(target).Should().BeTrue();
    }

    [Theory]
    [InlineData(SessionStatus.Scheduled)]
    [InlineData(SessionStatus.Preparing)]
    [InlineData(SessionStatus.Paused)]
    public void CanTransitionTo_InvalidTargets_ShouldReturnFalse(SessionStatus target)
    {
        _state.CanTransitionTo(target).Should().BeFalse();
    }

    [Fact]
    public void OnEnter_ShouldNotModifyContext()
    {
        var session = Session.Create("Test", "123456", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Mission", "Trivia", 1, Guid.NewGuid().ToString("N")) });
        SetStartedAt(session, DateTime.UtcNow);
        var originalStartedAt = session.StartedAt;
        var originalEndedAt = session.EndedAt;

        _state.OnEnter(session);

        session.StartedAt.Should().Be(originalStartedAt);
        session.EndedAt.Should().Be(originalEndedAt);
    }

    [Fact]
    public void OnExit_ShouldNotModifyContext()
    {
        var session = Session.Create("Test", "123456", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Mission", "Trivia", 1, Guid.NewGuid().ToString("N")) });
        SetStartedAt(session, DateTime.UtcNow);

        _state.OnExit(session);

        session.StartedAt.Should().NotBeNull();
    }

    private static void SetStartedAt(Session session, DateTime value)
    {
        typeof(Session).GetProperty("StartedAt")!.SetValue(session, value);
    }
}