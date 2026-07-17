using MediatR;
using Microsoft.Extensions.Logging;
using Sessions.Application.Common.Interfaces;
using Sessions.Domain.Entities;
using Sessions.Domain.Enums;

namespace Sessions.Application.Sessions.ValidateQr;

public class ValidateQrCommandHandler : IRequestHandler<ValidateQrCommand, ValidateQrResult>
{
    private readonly ISessionRepository _repository;
    private readonly IGameSessionFacade _facade;
    private readonly IGameNotifier _notifier;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<ValidateQrCommandHandler> _logger;

    public ValidateQrCommandHandler(
        ISessionRepository repository,
        IGameSessionFacade facade,
        IGameNotifier notifier,
        IEventPublisher eventPublisher,
        ILogger<ValidateQrCommandHandler> logger)
    {
        _repository = repository;
        _facade = facade;
        _notifier = notifier;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    // RF-14: publish a domain event to RabbitMQ whenever evidence is registered, mirroring the
    // pattern already used for session status changes. Best-effort — a broker outage must not
    // block the participant's QR scan.
    private async Task PublishEvidenceSubmittedAsync(
        Guid sessionId, Guid userId, Guid? teamId, Guid stageId, bool isValid, int? scoreDelta, CancellationToken ct)
    {
        try
        {
            await _eventPublisher.PublishAsync("evidence.submitted", new
            {
                SessionId = sessionId,
                UserId = userId,
                TeamId = teamId,
                StageId = stageId,
                IsValid = isValid,
                ScoreDelta = scoreDelta,
                OccurredAt = DateTime.UtcNow
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish evidence.submitted event for session {SessionId}", sessionId);
        }
    }

    public async Task<ValidateQrResult> Handle(ValidateQrCommand command, CancellationToken ct)
    {
        var session = await _repository.GetByIdWithStagesAsync(command.SessionId, ct);

        if (session is null)
            return new ValidateQrResult(false, false, 0, 0, false, ErrorMessage: "Session not found");

        if (session.Status != SessionStatus.Active)
            return new ValidateQrResult(false, false, 0, session.Stages.Count, false,
                ErrorMessage: $"Session is not active (status: {session.Status})");

        var participant = await _repository.GetParticipantAsync(command.SessionId, command.UserId, ct);

        if (participant is null)
            return new ValidateQrResult(false, false, 0, session.Stages.Count, false,
                ErrorMessage: "Participant not found in this session");

        if (participant.HasCompleted)
            return new ValidateQrResult(true, false, participant.CurrentStageOrder,
                session.Stages.Count, true);

        var sortedStages = session.Stages.OrderBy(s => s.Order).ToList();
        var stageIndex = participant.CurrentStageOrder;

        if (stageIndex >= sortedStages.Count)
            return new ValidateQrResult(false, false, participant.CurrentStageOrder,
                sortedStages.Count, false, ErrorMessage: "No pending stage found");

        var currentStage = sortedStages[stageIndex];

        if (!currentStage.ValidateQrToken(command.ScannedStageId, command.ScannedToken))
        {
            _logger.LogWarning(
                "QR validation failed: SessionId={SessionId}, UserId={UserId}, StageIndex={StageIndex}",
                command.SessionId, command.UserId, stageIndex);

            // RF-09: persist the rejected evidence attempt so operators can audit it later.
            await _repository.AddAuditEventAsync(SessionAuditEvent.Create(
                command.SessionId,
                SessionAuditEventTypes.EvidenceRejected,
                $"QR inválido para la etapa '{currentStage.StageName}'",
                userId: command.UserId), ct);

            await PublishEvidenceSubmittedAsync(
                command.SessionId, command.UserId, teamId: null, stageId: currentStage.MissionStageId,
                isValid: false, scoreDelta: null, ct);

            return new ValidateQrResult(false, false, participant.CurrentStageOrder,
                sortedStages.Count, false, ErrorMessage: "Invalid QR code for current stage");
        }

        bool isLastStage = stageIndex == sortedStages.Count - 1;
        int newOrder = stageIndex + 1;

        // Team members share progress and points: one scan advances (and scores for) the whole team.
        var participantTeam = await _repository.GetParticipantTeamInSessionAsync(command.SessionId, command.UserId, ct);
        var teammates = new List<SessionParticipant>();
        if (participantTeam is not null)
        {
            foreach (var member in participantTeam.Members.Where(m => m.UserId != command.UserId))
            {
                var mate = await _repository.GetParticipantAsync(command.SessionId, member.UserId, ct);
                if (mate is not null && !mate.HasCompleted) teammates.Add(mate);
            }
        }

        // Award points for every successfully scanned QR — scaled by the mission's difficulty.
        int scanPoints = currentStage.BaseScanPoints;
        if (participantTeam is not null)
            await _repository.AddTeamScoreAsync(participantTeam.Id, scanPoints, ct);
        else
            participant.AddScore(scanPoints);

        // RF-09: evidence with validation status — record the successful QR scan.
        await _repository.AddAuditEventAsync(SessionAuditEvent.Create(
            command.SessionId,
            SessionAuditEventTypes.EvidenceValidated,
            $"QR válido para la etapa '{currentStage.StageName}'",
            teamId: participantTeam?.Id,
            userId: command.UserId,
            scoreDelta: scanPoints), ct);

        // RF-14: publish the domain event to RabbitMQ.
        await PublishEvidenceSubmittedAsync(
            command.SessionId, command.UserId, participantTeam?.Id, currentStage.MissionStageId,
            isValid: true, scoreDelta: scanPoints, ct);

        participant.AdvanceStage();
        foreach (var mate in teammates) mate.CatchUpTo(newOrder);

        // Notify team members that one of them scanned and advanced
        if (participantTeam is not null)
        {
            await _notifier.NotifyTeamStageAdvancedAsync(
                command.SessionId,
                participantTeam.Id,
                participant.CurrentStageOrder + 1,
                sortedStages.Count,
                ct);
        }

        if (isLastStage)
        {
            // Podium bonus: 1st=300, 2nd=200, 3rd=100 based on which team/individual finishes first
            var allTeams = await _repository.GetTeamsBySessionIdAsync(command.SessionId, ct);
            Guid GroupOf(Guid userId) => allTeams.FirstOrDefault(t => t.Members.Any(m => m.UserId == userId))?.Id ?? userId;
            var myGroup = GroupOf(command.UserId);
            int completedBefore = session.Participants
                .Where(p => p.HasCompleted && GroupOf(p.UserId) != myGroup)
                .Select(p => GroupOf(p.UserId))
                .Distinct()
                .Count();
            int bonus = completedBefore switch { 0 => 300, 1 => 200, 2 => 100, _ => 0 };
            if (bonus > 0)
            {
                if (participantTeam is not null)
                    await _repository.AddTeamScoreAsync(participantTeam.Id, bonus, ct);
                else
                    participant.AddScore(bonus);

                // RB-07: podium bonus must be traceable to its origin, same as any other score change.
                await _repository.AddAuditEventAsync(SessionAuditEvent.Create(
                    command.SessionId,
                    SessionAuditEventTypes.EvidenceValidated,
                    $"Bono de podio (posición {completedBefore + 1}) por completar la misión",
                    teamId: participantTeam?.Id,
                    userId: command.UserId,
                    scoreDelta: bonus), ct);
            }

            participant.Complete();
            foreach (var mate in teammates) mate.Complete();

            await _repository.UpdateParticipantAsync(participant, ct);
            foreach (var mate in teammates) await _repository.UpdateParticipantAsync(mate, ct);

            _logger.LogInformation(
                "Participant {UserId} completed all stages (position {Position}) in session {SessionId}",
                command.UserId, completedBefore + 1, command.SessionId);

            var rankingOnComplete = await _repository.GetSessionRankingAsync(command.SessionId, ct);
            await _notifier.NotifyRankingUpdatedAsync(command.SessionId, rankingOnComplete, ct);

            bool allDone = session.Participants.All(p => p.HasCompleted);
            if (allDone && session.Status == SessionStatus.Active)
                await _facade.TransitionAndNotify(command.SessionId, "Finished", ct);
            else
                await AutoAdvanceSessionStageIfEveryoneCaughtUp(session, sortedStages.Count, ct);

            return new ValidateQrResult(true, true, participant.CurrentStageOrder,
                sortedStages.Count, true);
        }

        await _repository.UpdateParticipantAsync(participant, ct);
        foreach (var mate in teammates) await _repository.UpdateParticipantAsync(mate, ct);

        _logger.LogInformation(
            "Participant {UserId} advanced to stage {NewOrder} in session {SessionId}",
            command.UserId, participant.CurrentStageOrder, command.SessionId);

        var rankingEntries = await _repository.GetSessionRankingAsync(command.SessionId, ct);
        await _notifier.NotifyRankingUpdatedAsync(command.SessionId, rankingEntries, ct);

        await AutoAdvanceSessionStageIfEveryoneCaughtUp(session, sortedStages.Count, ct);

        return new ValidateQrResult(true, true, participant.CurrentStageOrder,
            sortedStages.Count, false);
    }

    // The session's own CurrentStageOrder (what the operator dashboard and its per-mission
    // timer follow) previously only moved via a manual click or a full mission timeout —
    // participants racing through QR-gated Treasure stages left it stuck, so the operator had
    // no idea a stage was already done. Advance it automatically once every participant has
    // scanned past it, so the dashboard follows real progress instead of lagging behind.
    private async Task AutoAdvanceSessionStageIfEveryoneCaughtUp(Session session, int totalStages, CancellationToken ct)
    {
        while (session.Status == SessionStatus.Active &&
               session.CurrentStageOrder + 1 < totalStages &&
               session.Participants.Count > 0 &&
               session.Participants.All(p => p.CurrentStageOrder > session.CurrentStageOrder))
        {
            session.AdvanceStage();
            await _repository.UpdateAsync(session, ct);
            await _facade.NotifyStageAdvanced(session.Id, session.CurrentStageOrder, ct);
        }
    }
}
