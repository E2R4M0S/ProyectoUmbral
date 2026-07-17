using FluentAssertions;
using NSubstitute;
using Sessions.Application.Common.Interfaces;
using Sessions.Application.Sessions.Consult;
using Sessions.Domain.Entities;
using Sessions.Domain.Enums;
using Xunit;

namespace Sessions.Application.Tests.Consult;

public class GetSessionProgressQueryHandlerTests
{
    private readonly ISessionRepository _repo = Substitute.For<ISessionRepository>();
    private readonly GetSessionProgressQueryHandler _sut;

    public GetSessionProgressQueryHandlerTests()
    {
        _sut = new GetSessionProgressQueryHandler(_repo);
    }

    [Fact]
    public async Task Handle_SessionNotFound_ShouldReturnNull()
    {
        _repo.GetByIdWithStagesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Session?)null);
        var result = await _sut.Handle(new GetSessionProgressQuery(Guid.NewGuid()), CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_SessionExists_ShouldReturnProgress()
    {
        var session = Session.Create("Test", "123456", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Mission", "Stage", "Trivia", 1, "test-token") });
        _repo.GetByIdWithStagesAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        var result = await _sut.Handle(new GetSessionProgressQuery(session.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Test");
        result.Status.Should().Be(SessionStatus.Scheduled.ToString());
    }

    [Fact]
    public async Task Handle_SessionPaused_ShouldFreezeElapsedAtPauseMoment()
    {
        var session = Session.Create("Test", "123456", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Mission", "Stage", "Trivia", 1, "test-token") });
        var missionStart = DateTime.UtcNow.AddMinutes(-10);
        SetProperty(session, "StartedAt", missionStart);
        SetProperty(session, "CurrentMissionStartedAt", missionStart);
        SetProperty(session, "Status", SessionStatus.Paused);
        // Paused 4 minutes ago — elapsed should freeze at ~6 minutes (10 - 4), not keep
        // growing with real time even though "now" is later.
        var pausedAt = DateTime.UtcNow.AddMinutes(-4);
        SetProperty(session, "PausedAt", pausedAt);
        _repo.GetByIdWithStagesAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        var result = await _sut.Handle(new GetSessionProgressQuery(session.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.ElapsedSeconds.Should().BeInRange(355, 365); // ~6 minutes, allow slack
        result.CurrentMissionElapsedSeconds.Should().BeInRange(355, 365);
    }

    private static void SetProperty(Session session, string name, object value)
    {
        typeof(Session).GetProperty(name)!.SetValue(session, value);
    }
}

