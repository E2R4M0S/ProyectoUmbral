using FluentAssertions;
using Sessions.Domain.Entities;
using Sessions.Domain.Enums;
using Sessions.Domain.States;
using Xunit;

namespace Sessions.Domain.Tests.States;

public class ScheduledStateTests
{
    private readonly ScheduledState _state = new();

    [Fact]
    public void Status_ShouldReturnScheduled()
    {
        _state.Status.Should().Be(SessionStatus.Scheduled);
    }

    [Theory]
    [InlineData(SessionStatus.Preparing)]
    [InlineData(SessionStatus.Cancelled)]
    public void CanTransitionTo_ValidTargets_ShouldReturnTrue(SessionStatus target)
    {
        _state.CanTransitionTo(target).Should().BeTrue();
    }

    [Theory]
    [InlineData(SessionStatus.Scheduled)]
    [InlineData(SessionStatus.Active)]
    [InlineData(SessionStatus.Paused)]
    [InlineData(SessionStatus.Finished)]
    public void CanTransitionTo_InvalidTargets_ShouldReturnFalse(SessionStatus target)
    {
        _state.CanTransitionTo(target).Should().BeFalse();
    }

    [Fact]
    public void OnEnter_ShouldNotModifyContext()
    {
        var session = Session.Create("Test", "123456", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), "Mission", "Trivia", 1) });
        var originalStartedAt = session.StartedAt;
        var originalEndedAt = session.EndedAt;

        _state.OnEnter(session);

        session.StartedAt.Should().Be(originalStartedAt);
        session.EndedAt.Should().Be(originalEndedAt);
    }

    [Fact]
    public void OnExit_ShouldNotModifyContext()
    {
        var session = Session.Create("Test", "123456", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), "Mission", "Trivia", 1) });

        _state.OnExit(session);

        session.StartedAt.Should().BeNull();
        session.EndedAt.Should().BeNull();
    }
}