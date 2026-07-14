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
                "QR validation failed: SessionId={SessionId}, UserId={UserId}, StageIndex={StageIndex}",
                command.SessionId, command.UserId, stageIndex);
            return new ValidateQrResult(false, false, participant.CurrentStageOrder,
                sortedStages.Count, false, ErrorMessage: "Invalid QR code for current stage");
        }

        bool isLastStage = stageIndex == sortedStages.Count - 1;

        // Award 100 pts for every successfully scanned QR
        participant.AddScore(100);
        participant.AdvanceStage();

        if (isLastStage)
        {
            // Podium bonus: 1st=300, 2nd=200, 3rd=100 based on who finishes first
            int completedBefore = session.Participants.Count(p => p.UserId != command.UserId && p.HasCompleted);
            int bonus = completedBefore switch { 0 => 300, 1 => 200, 2 => 100, _ => 0 };
            if (bonus > 0) participant.AddScore(bonus);

            participant.Complete();
            await _repository.UpdateParticipantAsync(participant, ct);

            _logger.LogInformation(
                "Participant {UserId} completed all stages (position {Position}) in session {SessionId}",
                command.UserId, completedBefore + 1, command.SessionId);

            await _notifier.NotifyRankingUpdatedAsync(
                command.SessionId,
                session.Participants.Select(p => (p.UserId, p.UserAlias, p.Score)),
                ct);

            bool allDone = session.Participants.All(p => p.UserId == command.UserId || p.HasCompleted);
            if (allDone && session.Status == SessionStatus.Active)
                await _facade.TransitionAndNotify(command.SessionId, "Finished", ct);

            return new ValidateQrResult(true, true, participant.CurrentStageOrder,
                sortedStages.Count, true);
        }

        await _repository.UpdateParticipantAsync(participant, ct);

        _logger.LogInformation(
            "Participant {UserId} advanced to stage {NewOrder} in session {SessionId}",
            command.UserId, participant.CurrentStageOrder, command.SessionId);

        await _notifier.NotifyRankingUpdatedAsync(
            command.SessionId,
            session.Participants.Select(p => (p.UserId, p.UserAlias, p.Score)),
            ct);

        return new ValidateQrResult(true, true, participant.CurrentStageOrder,
            sortedStages.Count, false);
    }
}
