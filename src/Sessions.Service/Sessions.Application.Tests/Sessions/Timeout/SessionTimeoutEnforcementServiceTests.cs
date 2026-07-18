using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Sessions.Application.Common.Interfaces;
using Sessions.Application.Sessions.Timeout;
using Sessions.Domain.Entities;
using Sessions.Domain.Enums;
using Xunit;

namespace Sessions.Application.Tests.Sessions.Timeout;

public class SessionTimeoutEnforcementServiceTests
{
    private readonly ISessionRepository _repository = Substitute.For<ISessionRepository>();
    private readonly IGameSessionFacade _facade = Substitute.For<IGameSessionFacade>();
    private readonly IGameNotifier _notifier = Substitute.For<IGameNotifier>();
    private readonly IMissionCatalogService _missionCatalogService = Substitute.For<IMissionCatalogService>();
    private readonly ILogger<SessionTimeoutEnforcementService> _logger =
        Substitute.For<ILogger<SessionTimeoutEnforcementService>>();
    private readonly SessionTimeoutEnforcementService _sut;

    public SessionTimeoutEnforcementServiceTests()
    {
        _repository.GetAuditTrailAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<SessionAuditEvent>());
        _missionCatalogService.GetAutomaticCluesAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AutomaticClueSummary>());
        _sut = new SessionTimeoutEnforcementService(_repository, _facade, _notifier, _missionCatalogService, _logger);
    }

    private static Session BuildActiveSessionWithTwoMissions(int firstMissionTimeMinutes, DateTime currentMissionStartedAt)
    {
        var mission1 = Guid.NewGuid();
        var mission2 = Guid.NewGuid();
        var stages = new List<SessionStage>
        {
            SessionStage.Create(mission1, Guid.NewGuid(), "Mission 1", "Stage 1", "Trivia", 1, "tok1", firstMissionTimeMinutes),
            SessionStage.Create(mission2, Guid.NewGuid(), "Mission 2", "Stage 2", "Trivia", 2, "tok2", 30),
        };
        var session = Session.Create("Timeout Session", "111111", stages);
        session.TransitionTo(SessionStatus.Preparing);
        session.AddParticipant(Guid.NewGuid());
        session.TransitionTo(SessionStatus.Active);

        typeof(Session).GetProperty(nameof(Session.CurrentMissionStartedAt))!
            .SetValue(session, currentMissionStartedAt);

        return session;
    }

    private static Session BuildActiveSingleMissionSession(int timeMinutes, DateTime currentMissionStartedAt)
    {
        var missionId = Guid.NewGuid();
        var stages = new List<SessionStage>
        {
            SessionStage.Create(missionId, Guid.NewGuid(), "Only Mission", "Stage 1", "Trivia", 1, "tok1", timeMinutes)
        };
        var session = Session.Create("Timeout Session", "222222", stages);
        session.TransitionTo(SessionStatus.Preparing);
        session.AddParticipant(Guid.NewGuid());
        session.TransitionTo(SessionStatus.Active);

        typeof(Session).GetProperty(nameof(Session.CurrentMissionStartedAt))!
            .SetValue(session, currentMissionStartedAt);

        return session;
    }

    [Fact]
    public async Task EnforceSessionTimeoutAsync_WhenSessionNotFound_ShouldDoNothing()
    {
        _repository.GetByIdWithStagesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Session?)null);

        await _sut.EnforceSessionTimeoutAsync(Guid.NewGuid(), CancellationToken.None);

        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Session>(), Arg.Any<CancellationToken>());
        await _facade.DidNotReceive().TransitionAndNotify(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnforceSessionTimeoutAsync_WhenMissionNotYetTimedOut_ShouldDoNothing()
    {
        var session = BuildActiveSessionWithTwoMissions(15, DateTime.UtcNow.AddMinutes(-1));
        _repository.GetByIdWithStagesAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        await _sut.EnforceSessionTimeoutAsync(session.Id, CancellationToken.None);

        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Session>(), Arg.Any<CancellationToken>());
        await _facade.DidNotReceive().TransitionAndNotify(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _facade.DidNotReceive().NotifyStageAdvanced(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnforceSessionTimeoutAsync_WhenTimeMinutesIsZero_ShouldTreatAsUnlimitedAndDoNothing()
    {
        var session = BuildActiveSingleMissionSession(0, DateTime.UtcNow.AddHours(-5));
        _repository.GetByIdWithStagesAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        await _sut.EnforceSessionTimeoutAsync(session.Id, CancellationToken.None);

        await _facade.DidNotReceive().TransitionAndNotify(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnforceSessionTimeoutAsync_WhenNotLastMissionTimedOut_ShouldAdvanceToNextMissionAndNotify()
    {
        var session = BuildActiveSessionWithTwoMissions(15, DateTime.UtcNow.AddMinutes(-20));
        _repository.GetByIdWithStagesAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        await _sut.EnforceSessionTimeoutAsync(session.Id, CancellationToken.None);

        session.CurrentStageOrder.Should().Be(1); // moved to stage index 1 = second mission's first stage (Order 2)
        session.Stages[session.CurrentStageOrder].MissionTitle.Should().Be("Mission 2");
        await _repository.Received(1).UpdateAsync(session, Arg.Any<CancellationToken>());
        await _repository.Received(1).AddAuditEventAsync(
            Arg.Is<SessionAuditEvent>(e => e.SessionId == session.Id && e.EventType == SessionAuditEventTypes.StatusChanged),
            Arg.Any<CancellationToken>());
        await _facade.Received(1).NotifyStageAdvanced(session.Id, 1, Arg.Any<CancellationToken>());
        await _facade.DidNotReceive().TransitionAndNotify(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnforceSessionTimeoutAsync_WhenLastMissionTimedOut_ShouldFinishSession()
    {
        var session = BuildActiveSingleMissionSession(15, DateTime.UtcNow.AddMinutes(-20));
        _repository.GetByIdWithStagesAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        await _sut.EnforceSessionTimeoutAsync(session.Id, CancellationToken.None);

        await _facade.Received(1).TransitionAndNotify(session.Id, "Finished", Arg.Any<CancellationToken>(), skipOwnershipCheck: true);
        await _repository.Received(1).AddAuditEventAsync(
            Arg.Is<SessionAuditEvent>(e => e.SessionId == session.Id && e.EventType == SessionAuditEventTypes.StatusChanged),
            Arg.Any<CancellationToken>());
        await _facade.DidNotReceive().NotifyStageAdvanced(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnforceActiveSessionsAsync_ShouldEnforceEveryActiveSessionReturnedByRepository()
    {
        var session1 = BuildActiveSingleMissionSession(15, DateTime.UtcNow.AddMinutes(-20));
        var session2 = BuildActiveSessionWithTwoMissions(15, DateTime.UtcNow.AddMinutes(-20));

        _repository.GetSessionsAsync(null, "Active", null, 1, 1000, Arg.Any<CancellationToken>())
            .Returns((new List<Session> { session1, session2 }, 2));
        _repository.GetByIdWithStagesAsync(session1.Id, Arg.Any<CancellationToken>()).Returns(session1);
        _repository.GetByIdWithStagesAsync(session2.Id, Arg.Any<CancellationToken>()).Returns(session2);

        await _sut.EnforceActiveSessionsAsync(CancellationToken.None);

        await _facade.Received(1).TransitionAndNotify(session1.Id, "Finished", Arg.Any<CancellationToken>(), skipOwnershipCheck: true);
        await _facade.Received(1).NotifyStageAdvanced(session2.Id, Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnforceActiveSessionsAsync_WhenOneSessionThrows_ShouldStillProcessTheOthers()
    {
        var faultySession = BuildActiveSingleMissionSession(15, DateTime.UtcNow.AddMinutes(-20));
        var healthySession = BuildActiveSingleMissionSession(15, DateTime.UtcNow.AddMinutes(-20));

        _repository.GetSessionsAsync(null, "Active", null, 1, 1000, Arg.Any<CancellationToken>())
            .Returns((new List<Session> { faultySession, healthySession }, 2));
        _repository.GetByIdWithStagesAsync(faultySession.Id, Arg.Any<CancellationToken>())
            .Returns<Session?>(_ => throw new InvalidOperationException("boom"));
        _repository.GetByIdWithStagesAsync(healthySession.Id, Arg.Any<CancellationToken>()).Returns(healthySession);

        await _sut.EnforceActiveSessionsAsync(CancellationToken.None);

        await _facade.Received(1).TransitionAndNotify(healthySession.Id, "Finished", Arg.Any<CancellationToken>(), skipOwnershipCheck: true);
    }

    private static Session BuildActiveTreasureSession(
        int timeMinutes, DateTime currentMissionStartedAt, out Guid missionId, out Guid missionStageId)
    {
        missionId = Guid.NewGuid();
        missionStageId = Guid.NewGuid();
        var stages = new List<SessionStage>
        {
            SessionStage.Create(missionId, missionStageId, "Treasure Mission", "Stage 1", "Treasure", 1, "tok1", timeMinutes)
        };
        var session = Session.Create("Treasure Session", "333333", stages);
        session.TransitionTo(SessionStatus.Preparing);
        session.AddParticipant(Guid.NewGuid());
        session.TransitionTo(SessionStatus.Active);

        typeof(Session).GetProperty(nameof(Session.CurrentMissionStartedAt))!
            .SetValue(session, currentMissionStartedAt);

        return session;
    }

    [Fact]
    public async Task EnforceAutomaticClueReleaseAsync_WhenStageIsNotTreasure_ShouldDoNothing()
    {
        var session = BuildActiveSingleMissionSession(30, DateTime.UtcNow.AddMinutes(-20));
        _repository.GetByIdWithStagesAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        await _sut.EnforceAutomaticClueReleaseAsync(session.Id, CancellationToken.None);

        await _missionCatalogService.DidNotReceive().GetAutomaticCluesAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnforceAutomaticClueReleaseAsync_WhenFirstFiveMinuteMarkNotReached_ShouldNotRelease()
    {
        var session = BuildActiveTreasureSession(30, DateTime.UtcNow.AddMinutes(-2), out var missionId, out var stageId);
        _repository.GetByIdWithStagesAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        await _sut.EnforceAutomaticClueReleaseAsync(session.Id, CancellationToken.None);

        await _missionCatalogService.DidNotReceive().GetAutomaticCluesAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _facade.DidNotReceive().ReleaseClueAndNotify(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnforceAutomaticClueReleaseAsync_WhenThresholdReachedAndClueNotYetReleased_ShouldReleaseAndRecordAudit()
    {
        var session = BuildActiveTreasureSession(30, DateTime.UtcNow.AddMinutes(-16), out var missionId, out var stageId);
        _repository.GetByIdWithStagesAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        var clueId = Guid.NewGuid();
        _missionCatalogService.GetAutomaticCluesAsync(missionId, stageId, Arg.Any<CancellationToken>())
            .Returns(new List<AutomaticClueSummary> { new(clueId, "Busca bajo la piedra", null) });

        await _sut.EnforceAutomaticClueReleaseAsync(session.Id, CancellationToken.None);

        await _facade.Received(1).ReleaseClueAndNotify(
            session.Id, clueId, null, null, "Busca bajo la piedra", null, Arg.Any<CancellationToken>());
        await _repository.Received(1).AddAuditEventAsync(
            Arg.Is<SessionAuditEvent>(e =>
                e.SessionId == session.Id &&
                e.EventType == SessionAuditEventTypes.AutomaticClueReleased &&
                e.ClueId == clueId),
            Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().ApplyCluePenaltyAsync(
            Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnforceAutomaticClueReleaseAsync_WhenClueAlreadyReleased_ShouldNotReleaseAgain()
    {
        var session = BuildActiveTreasureSession(30, DateTime.UtcNow.AddMinutes(-16), out var missionId, out var stageId);
        _repository.GetByIdWithStagesAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        var clueId = Guid.NewGuid();
        _missionCatalogService.GetAutomaticCluesAsync(missionId, stageId, Arg.Any<CancellationToken>())
            .Returns(new List<AutomaticClueSummary> { new(clueId, "Busca bajo la piedra", null) });
        _repository.GetAuditTrailAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(new List<SessionAuditEvent>
            {
                SessionAuditEvent.Create(session.Id, SessionAuditEventTypes.AutomaticClueReleased, "ya liberada", clueId: clueId)
            });

        await _sut.EnforceAutomaticClueReleaseAsync(session.Id, CancellationToken.None);

        await _facade.DidNotReceive().ReleaseClueAndNotify(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnforceAutomaticClueReleaseAsync_WhenClueHasPenalty_ShouldApplyPenaltyAndNotifyRanking()
    {
        var session = BuildActiveTreasureSession(30, DateTime.UtcNow.AddMinutes(-16), out var missionId, out var stageId);
        _repository.GetByIdWithStagesAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        var clueId = Guid.NewGuid();
        _missionCatalogService.GetAutomaticCluesAsync(missionId, stageId, Arg.Any<CancellationToken>())
            .Returns(new List<AutomaticClueSummary> { new(clueId, "Busca bajo la piedra", 20) });

        await _sut.EnforceAutomaticClueReleaseAsync(session.Id, CancellationToken.None);

        await _repository.Received(1).ApplyCluePenaltyAsync(
            session.Id, null, null, 20, Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _notifier.Received(1).NotifySessionRankingUpdatedAsync(
            session.Id, Arg.Any<IEnumerable<SessionRankingEntry>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnforceAutomaticClueReleaseAsync_WithTwoClues_ReleasesOnlyTheOnesWhoseFiveMinuteMarkPassed()
    {
        // 7 minutes in: only the 5-minute mark has passed, so only the 1st clue is due —
        // the 2nd (due at 10 minutes) must stay held back.
        var session = BuildActiveTreasureSession(30, DateTime.UtcNow.AddMinutes(-7), out var missionId, out var stageId);
        _repository.GetByIdWithStagesAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        var firstClueId = Guid.NewGuid();
        var secondClueId = Guid.NewGuid();
        _missionCatalogService.GetAutomaticCluesAsync(missionId, stageId, Arg.Any<CancellationToken>())
            .Returns(new List<AutomaticClueSummary>
            {
                new(firstClueId, "Primera pista", null),
                new(secondClueId, "Segunda pista", null),
            });

        await _sut.EnforceAutomaticClueReleaseAsync(session.Id, CancellationToken.None);

        await _facade.Received(1).ReleaseClueAndNotify(
            session.Id, firstClueId, null, null, "Primera pista", null, Arg.Any<CancellationToken>());
        await _facade.DidNotReceive().ReleaseClueAndNotify(
            session.Id, secondClueId, Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnforceAutomaticClueReleaseAsync_WithTwoClues_ReleasesBothOncePastTenMinutes()
    {
        var session = BuildActiveTreasureSession(30, DateTime.UtcNow.AddMinutes(-11), out var missionId, out var stageId);
        _repository.GetByIdWithStagesAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        var firstClueId = Guid.NewGuid();
        var secondClueId = Guid.NewGuid();
        _missionCatalogService.GetAutomaticCluesAsync(missionId, stageId, Arg.Any<CancellationToken>())
            .Returns(new List<AutomaticClueSummary>
            {
                new(firstClueId, "Primera pista", null),
                new(secondClueId, "Segunda pista", null),
            });

        await _sut.EnforceAutomaticClueReleaseAsync(session.Id, CancellationToken.None);

        await _facade.Received(1).ReleaseClueAndNotify(
            session.Id, firstClueId, null, null, "Primera pista", null, Arg.Any<CancellationToken>());
        await _facade.Received(1).ReleaseClueAndNotify(
            session.Id, secondClueId, null, null, "Segunda pista", null, Arg.Any<CancellationToken>());
    }
}
