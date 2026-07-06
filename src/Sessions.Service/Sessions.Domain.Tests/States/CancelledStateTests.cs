using System.Reflection;
using FluentAssertions;
using Sessions.Domain.Entities;
using Sessions.Domain.Enums;
using Sessions.Domain.States;
using Xunit;

namespace Sessions.Domain.Tests.States;

public class CancelledStateTests
{
    private readonly CancelledState _state = new();

    [Fact]
    public void Status_ShouldReturnCancelled()
    {
        _state.Status.Should().Be(SessionStatus.Cancelled);
    }

    [Theory]
    [InlineData(SessionStatus.Scheduled)]
    [InlineData(SessionStatus.Preparing)]
    [InlineData(SessionStatus.Active)]
    [InlineData(SessionStatus.Paused)]
    [InlineData(SessionStatus.Finished)]
    [InlineData(SessionStatus.Cancelled)]
    public void CanTransitionTo_AnyTarget_ShouldReturnFalse(SessionStatus target)
    {
        _state.CanTransitionTo(target).Should().BeFalse();
    }

    [Fact]
    public void OnEnter_ShouldSetEndedAt()
    {
        var session = Session.Create("Test", "123456", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Mission", "Trivia", 1, Guid.NewGuid().ToString("N")) });
        SetStartedAt(session, DateTime.UtcNow);
        session.EndedAt.Should().BeNull();

        _state.OnEnter(session);

        session.EndedAt.Should().NotBeNull();
        session.EndedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void OnExit_ShouldNotModifyContext()
    {
        var session = Session.Create("Test", "123456", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Mission", "Trivia", 1, Guid.NewGuid().ToString("N")) });
        SetStartedAt(session, DateTime.UtcNow);
        SetEndedAt(session, DateTime.UtcNow);

        _state.OnExit(session);

        session.StartedAt.Should().NotBeNull();
        session.EndedAt.Should().NotBeNull();
    }

    private static void SetStartedAt(Session session, DateTime value)
    {
        typeof(Session).GetProperty("StartedAt")!.SetValue(session, value);
    }

    private static void SetEndedAt(Session session, DateTime value)
    {
        typeof(Session).GetProperty("EndedAt")!.SetValue(session, value);
    }
}