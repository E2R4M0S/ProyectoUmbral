using System.Reflection;
using FluentAssertions;
using Sessions.Domain.Entities;
using Sessions.Domain.Enums;
using Sessions.Domain.States;
using Xunit;

namespace Sessions.Domain.Tests.States;

public class ActiveStateTests
{
    private readonly ActiveState _state = new();

    [Fact]
    public void Status_ShouldReturnActive()
    {
        _state.Status.Should().Be(SessionStatus.Active);
    }

    [Theory]
    [InlineData(SessionStatus.Paused)]
    [InlineData(SessionStatus.Finished)]
    [InlineData(SessionStatus.Cancelled)]
    public void CanTransitionTo_ValidTargets_ShouldReturnTrue(SessionStatus target)
    {
        _state.CanTransitionTo(target).Should().BeTrue();
    }

    [Theory]
    [InlineData(SessionStatus.Scheduled)]
    [InlineData(SessionStatus.Preparing)]
    [InlineData(SessionStatus.Active)]
    public void CanTransitionTo_InvalidTargets_ShouldReturnFalse(SessionStatus target)
    {
        _state.CanTransitionTo(target).Should().BeFalse();
    }

    [Fact]
    public void OnEnter_WhenStartedAtIsNull_ShouldSetStartedAt()
    {
        var session = Session.Create("Test", "123456", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Mission", "Stage", "Trivia", 1, "test-token") });
        session.StartedAt.Should().BeNull();

        _state.OnEnter(session);

        session.StartedAt.Should().NotBeNull();
        session.StartedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void OnEnter_WhenStartedAtAlreadySet_ShouldNotOverwrite()
    {
        var session = Session.Create("Test", "123456", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Mission", "Stage", "Trivia", 1, "test-token") });
        SetStartedAt(session, DateTime.UtcNow.AddHours(-1));
        var originalStartedAt = session.StartedAt;

        _state.OnEnter(session);

        session.StartedAt.Should().Be(originalStartedAt);
    }

    [Fact]
    public void OnExit_ShouldNotModifyContext()
    {
        var session = Session.Create("Test", "123456", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Mission", "Stage", "Trivia", 1, "test-token") });
        SetStartedAt(session, DateTime.UtcNow);

        _state.OnExit(session);

        session.StartedAt.Should().NotBeNull();
    }

    [Fact]
    public void OnEnter_WhenResumingFromPause_ShouldShiftCurrentMissionStartedAtAndClearPausedAt()
    {
        var session = Session.Create("Test", "123456", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Mission", "Stage", "Trivia", 1, "test-token") });
        var missionStart = DateTime.UtcNow.AddMinutes(-10);
        SetStartedAt(session, missionStart);
        SetProperty(session, "CurrentMissionStartedAt", missionStart);
        var pausedAt = DateTime.UtcNow.AddMinutes(-5); // paused 5 minutes ago
        SetProperty(session, "PausedAt", pausedAt);

        _state.OnEnter(session);

        session.PausedAt.Should().BeNull();
        // The anchor should have shifted forward by roughly the 5-minute paused duration.
        session.CurrentMissionStartedAt.Should().BeCloseTo(missionStart.Add(DateTime.UtcNow - pausedAt), TimeSpan.FromSeconds(2));
        session.TotalPausedSeconds.Should().BeGreaterThan(290); // ~5 minutes, allow test-run slack
    }

    [Fact]
    public void OnEnter_WhenNotPaused_ShouldNotChangePausedFields()
    {
        var session = Session.Create("Test", "123456", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Mission", "Stage", "Trivia", 1, "test-token") });
        SetStartedAt(session, DateTime.UtcNow.AddMinutes(-10));

        _state.OnEnter(session);

        session.PausedAt.Should().BeNull();
        session.TotalPausedSeconds.Should().Be(0);
    }

    private static void SetStartedAt(Session session, DateTime value)
    {
        typeof(Session).GetProperty("StartedAt")!.SetValue(session, value);
    }

    private static void SetProperty(Session session, string name, object value)
    {
        typeof(Session).GetProperty(name)!.SetValue(session, value);
    }
}
