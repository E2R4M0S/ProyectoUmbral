using MediatR;
using Microsoft.Extensions.Logging;
using Sessions.Application.Common.Interfaces;
using Sessions.Domain.Enums;

namespace Sessions.Application.Sessions.ValidateQr;

public class ValidateQrCommandHandler : IRequestHandler<ValidateQrCommand, ValidateQrResult>
{
    private readonly ISessionRepository _repository;
    private readonly IGameSessionFacade _facade;
    private readonly IGameNotifier _notifier;
    private readonly ILogger<ValidateQrCommandHandler> _logger;

    public ValidateQrCommandHandler(
        ISessionRepository repository,
        IGameSessionFacade facade,
        IGameNotifier notifier,
        ILogger<ValidateQrCommandHandler> logger)
    {
        _repository = repository;
        _facade = facade;
        _notifier = notifier;
        _logger = logger;
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
                "QR validation failed: SessionId={SessionId}, UserId={UserId}, StageIndex={StageIndex}, " +
                "ScannedStageId={ScannedStageId}, StoredMissionStageId={StoredMissionStageId}, " +
                "ScannedTokenLength={ScannedTokenLength}, StoredTokenLength={StoredTokenLength}, " +
                "TokensMatch={TokensMatch}",
                command.SessionId, command.UserId, stageIndex,
                command.ScannedStageId, currentStage.MissionStageId,
                command.ScannedToken?.Length ?? -1, currentStage.QrToken?.Length ?? -1,
                command.ScannedToken == currentStage.QrToken);
            return new ValidateQrResult(false, false, participant.CurrentStageOrder,
                sortedStages.Count, false, ErrorMessage: "Invalid QR code for current stage");
        }

        bool isLastStage = stageIndex == sortedStages.Count - 1;

        // A gate occurs when crossing a mission boundary or reaching the final stage
        bool isGate = isLastStage
            || sortedStages[stageIndex + 1].MissionId != currentStage.MissionId;

        int totalParticipants = session.Participants.Count;
        int threshold = Math.Max(1, Math.Min(3, totalParticipants));

        if (isGate)
        {
            // Count participants who already passed this gate (before this one)
            int alreadyPassed = session.Participants.Count(p =>
                p.UserId != command.UserId && p.CurrentStageOrder > stageIndex);

            if (alreadyPassed >= threshold)
            {
                // Gate already closed — this participant is eliminated from advancing
                _logger.LogInformation(
                    "Participant {UserId} arrived too late at gate {StageIndex} in session {SessionId}",
                    command.UserId, stageIndex, command.SessionId);

                return new ValidateQrResult(false, false, participant.CurrentStageOrder,
                    sortedStages.Count, isLastStage,
                    IsEliminated: true,
                    GateThreshold: threshold,
                    ErrorMessage: $"Gate closed — top {threshold} already through");
            }

            participant.AdvanceStage();
            int newCount = alreadyPassed + 1;
            bool gateOpens = newCount >= threshold;

            if (gateOpens)
            {
                if (isLastStage)
                {
                    participant.Complete();
                    await _repository.UpdateParticipantAsync(participant, ct);

                    _logger.LogInformation(
                        "Participant {UserId} completed all stages — finishing session {SessionId}",
                        command.UserId, command.SessionId);

                    if (session.Status == SessionStatus.Active)
                        await _facade.TransitionAndNotify(command.SessionId, "Finished", ct);

                    return new ValidateQrResult(true, true, participant.CurrentStageOrder,
                        sortedStages.Count, true, IsAtGate: true, GateOpened: true,
                        GatePosition: newCount, GateThreshold: threshold);
                }
                else
                {
                    participant.ClearWaiting();
                    await _repository.UpdateParticipantAsync(participant, ct);

                    await _notifier.NotifyGateOpenedAsync(command.SessionId, stageIndex + 1, ct);

                    _logger.LogInformation(
                        "Gate opened at stage {StageIndex} in session {SessionId} — triggered by {UserId}",
                        stageIndex, command.SessionId, command.UserId);

                    return new ValidateQrResult(true, true, participant.CurrentStageOrder,
                        sortedStages.Count, false, IsAtGate: true, GateOpened: true,
                        GatePosition: newCount, GateThreshold: threshold);
                }
            }
            else
            {
                // This participant must wait at the gate
                participant.SetWaiting();
                await _repository.UpdateParticipantAsync(participant, ct);

                _logger.LogInformation(
                    "Participant {UserId} waiting at gate {StageIndex} ({Position}/{Threshold}) in session {SessionId}",
                    command.UserId, stageIndex, newCount, threshold, command.SessionId);

                return new ValidateQrResult(true, true, participant.CurrentStageOrder,
                    sortedStages.Count, isLastStage, IsAtGate: true, GateOpened: false,
                    GatePosition: newCount, GateThreshold: threshold);
            }
        }
        else
        {
            // Within same mission — advance immediately, no waiting
            participant.AdvanceStage();
            await _repository.UpdateParticipantAsync(participant, ct);

            _logger.LogInformation(
                "Participant {UserId} advanced to stage {NewOrder} (same mission) in session {SessionId}",
                command.UserId, participant.CurrentStageOrder, command.SessionId);

            return new ValidateQrResult(true, true, participant.CurrentStageOrder,
                sortedStages.Count, false, IsAtGate: false);
        }
    }
}
