using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Sessions.Application.Common.Interfaces;
using Sessions.Application.Sessions.ValidateQr;
using Sessions.Domain.Entities;
using Sessions.Domain.Enums;
using Xunit;

namespace Sessions.Application.Tests.Sessions.ValidateQr;

public class ValidateQrCommandHandlerTests
{
    private readonly ISessionRepository _repository = Substitute.For<ISessionRepository>();
    private readonly IGameSessionFacade _facade = Substitute.For<IGameSessionFacade>();
    private readonly IGameNotifier _notifier = Substitute.For<IGameNotifier>();
    private readonly IEventPublisher _eventPublisher = Substitute.For<IEventPublisher>();
    private readonly ILogger<ValidateQrCommandHandler> _logger =
        Substitute.For<ILogger<ValidateQrCommandHandler>>();
    private readonly ValidateQrCommandHandler _sut;

    public ValidateQrCommandHandlerTests()
    {
        _repository.GetTeamsBySessionIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<SessionTeam>());
        _sut = new ValidateQrCommandHandler(_repository, _facade, _notifier, _eventPublisher, _logger);
    }

    private static Session BuildActiveSession(IReadOnlyList<SessionStage> stages)
    {
        var session = Session.Create("Test Session", "111222", stages.ToList());
        session.TransitionTo(SessionStatus.Preparing);
        session.TransitionTo(SessionStatus.Active);
        return session;
    }

    private static SessionParticipant BuildParticipant(Guid sessionId, Guid userId)
        => SessionParticipant.Create(sessionId, userId);

    [Fact]
    public async Task Handle_SessionNotFound_ShouldReturnInvalidResult()
    {
        _repository.GetByIdWithStagesAsync(Arg.Any<Guid>(), default).Returns((Session?)null);

        var result = await _sut.Handle(new ValidateQrCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "token"), default);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_SessionNotActive_ShouldReturnInvalidResult()
    {
        var stageId = Guid.NewGuid();
        var stage = SessionStage.Create(Guid.NewGuid(), stageId, "M1", "Stage", "Treasure", 1, "tok");
        var session = Session.Create("Test", "123456", new List<SessionStage> { stage });
        // Session is still Scheduled (not Active)
        _repository.GetByIdWithStagesAsync(Arg.Any<Guid>(), default).Returns(session);

        var result = await _sut.Handle(new ValidateQrCommand(Guid.NewGuid(), Guid.NewGuid(), stageId, "tok"), default);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not active");
    }

    [Fact]
    public async Task Handle_InvalidQrToken_ShouldReturnInvalidResult()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var stageId = Guid.NewGuid();
        var stage = SessionStage.Create(Guid.NewGuid(), stageId, "M1", "Stage", "Treasure", 1, "correct-token");
        var session = BuildActiveSession(new[] { stage });
        _repository.GetByIdWithStagesAsync(Arg.Any<Guid>(), default).Returns(session);
        _repository.GetParticipantAsync(Arg.Any<Guid>(), userId, default)
            .Returns(BuildParticipant(sessionId, userId));

        var result = await _sut.Handle(new ValidateQrCommand(sessionId, userId, stageId, "wrong-token"), default);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid QR");
        await _repository.Received(1).AddAuditEventAsync(
            Arg.Is<SessionAuditEvent>(e => e.SessionId == sessionId && e.EventType == SessionAuditEventTypes.EvidenceRejected && e.UserId == userId),
            Arg.Any<CancellationToken>());
        await _eventPublisher.Received(1).PublishAsync(
            "evidence.submitted", Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidQrOnLastStage_ShouldCompleteAndFinishSession()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var stageId = Guid.NewGuid();
        var stage = SessionStage.Create(Guid.NewGuid(), stageId, "M1", "Stage", "Treasure", 1, "tok");
        var session = BuildActiveSession(new[] { stage });
        _repository.GetByIdWithStagesAsync(Arg.Any<Guid>(), default).Returns(session);
        _repository.GetParticipantAsync(Arg.Any<Guid>(), userId, default)
            .Returns(BuildParticipant(sessionId, userId));

        var result = await _sut.Handle(new ValidateQrCommand(sessionId, userId, stageId, "tok"), default);

        result.IsValid.Should().BeTrue();
        result.Advanced.Should().BeTrue();
        result.IsLastStage.Should().BeTrue();
        await _repository.Received(1).UpdateParticipantAsync(Arg.Any<SessionParticipant>(), Arg.Any<CancellationToken>());
        await _notifier.Received(1).NotifyRankingUpdatedAsync(sessionId, Arg.Any<IEnumerable<SessionRankingEntry>>(), Arg.Any<CancellationToken>());
        await _facade.Received(1).TransitionAndNotify(sessionId, "Finished", Arg.Any<CancellationToken>());
        await _eventPublisher.Received(1).PublishAsync(
            "evidence.submitted", Arg.Any<object>(), Arg.Any<CancellationToken>());
        // RB-07: the podium bonus (1st place = 300, since this participant is the only one to finish) must be traceable.
        await _repository.Received(1).AddAuditEventAsync(
            Arg.Is<SessionAuditEvent>(e => e.SessionId == sessionId && e.UserId == userId && e.ScoreDelta == 300 && e.Description.Contains("Bono de podio")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidQrNotLastStage_ShouldAdvanceAndNotifyRankingUpdated()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var stage1Id = Guid.NewGuid();
        var stage1 = SessionStage.Create(Guid.NewGuid(), stage1Id, "M1", "Stage", "Treasure", 1, "tok1");
        var stage2 = SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "M2", "Stage", "Treasure", 2, "tok2");
        var stage3 = SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "M3", "Stage", "Treasure", 3, "tok3");
        var session = BuildActiveSession(new[] { stage1, stage2, stage3 });
        _repository.GetByIdWithStagesAsync(Arg.Any<Guid>(), default).Returns(session);
        _repository.GetParticipantAsync(Arg.Any<Guid>(), userId, default)
            .Returns(BuildParticipant(sessionId, userId));

        var result = await _sut.Handle(new ValidateQrCommand(sessionId, userId, stage1Id, "tok1"), default);

        result.IsValid.Should().BeTrue();
        result.Advanced.Should().BeTrue();
        result.IsLastStage.Should().BeFalse();
        await _repository.Received(1).UpdateParticipantAsync(Arg.Any<SessionParticipant>(), Arg.Any<CancellationToken>());
        await _notifier.Received(1).NotifyRankingUpdatedAsync(sessionId, Arg.Any<IEnumerable<SessionRankingEntry>>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).AddAuditEventAsync(
            // Stage difficulty defaults to "Medium" (150 pts) when not specified.
            Arg.Is<SessionAuditEvent>(e => e.SessionId == sessionId && e.EventType == SessionAuditEventTypes.EvidenceValidated && e.UserId == userId && e.ScoreDelta == 150),
            Arg.Any<CancellationToken>());
        await _eventPublisher.Received(1).PublishAsync(
            "evidence.submitted", Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidQrDifficultyEasy_ShouldAward100Points()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var stageId = Guid.NewGuid();
        var stage = SessionStage.Create(Guid.NewGuid(), stageId, "M1", "Stage", "Treasure", 1, "tok", difficulty: "Easy");
        var stage2 = SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "M2", "Stage2", "Treasure", 2, "tok2", difficulty: "Easy");
        var session = BuildActiveSession(new[] { stage, stage2 });
        _repository.GetByIdWithStagesAsync(Arg.Any<Guid>(), default).Returns(session);
        _repository.GetParticipantAsync(Arg.Any<Guid>(), userId, default)
            .Returns(BuildParticipant(sessionId, userId));

        await _sut.Handle(new ValidateQrCommand(sessionId, userId, stageId, "tok"), default);

        await _repository.Received(1).AddAuditEventAsync(
            Arg.Is<SessionAuditEvent>(e => e.SessionId == sessionId && e.EventType == SessionAuditEventTypes.EvidenceValidated && e.UserId == userId && e.ScoreDelta == 100),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidQrDifficultyHard_ShouldAward200Points()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var stageId = Guid.NewGuid();
        var stage = SessionStage.Create(Guid.NewGuid(), stageId, "M1", "Stage", "Treasure", 1, "tok", difficulty: "Hard");
        var stage2 = SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "M2", "Stage2", "Treasure", 2, "tok2", difficulty: "Hard");
        var session = BuildActiveSession(new[] { stage, stage2 });
        _repository.GetByIdWithStagesAsync(Arg.Any<Guid>(), default).Returns(session);
        _repository.GetParticipantAsync(Arg.Any<Guid>(), userId, default)
            .Returns(BuildParticipant(sessionId, userId));

        await _sut.Handle(new ValidateQrCommand(sessionId, userId, stageId, "tok"), default);

        await _repository.Received(1).AddAuditEventAsync(
            Arg.Is<SessionAuditEvent>(e => e.SessionId == sessionId && e.EventType == SessionAuditEventTypes.EvidenceValidated && e.UserId == userId && e.ScoreDelta == 200),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidQrNotLastStage_ShouldReturnUpdatedStageOrder()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var stage1Id = Guid.NewGuid();
        var stage1 = SessionStage.Create(Guid.NewGuid(), stage1Id, "M1", "Stage", "Treasure", 1, "tok1");
        var stage2 = SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "M2", "Stage", "Treasure", 2, "tok2");
        var stage3 = SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "M3", "Stage", "Treasure", 3, "tok3");
        var session = BuildActiveSession(new[] { stage1, stage2, stage3 });
        _repository.GetByIdWithStagesAsync(Arg.Any<Guid>(), default).Returns(session);
        _repository.GetParticipantAsync(Arg.Any<Guid>(), userId, default)
            .Returns(BuildParticipant(sessionId, userId));

        var result = await _sut.Handle(new ValidateQrCommand(sessionId, userId, stage1Id, "tok1"), default);

        result.CurrentStageOrder.Should().Be(1);
        result.TotalStages.Should().Be(3);
    }
}
