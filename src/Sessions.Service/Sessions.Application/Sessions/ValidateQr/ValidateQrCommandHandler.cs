using MediatR;
using Microsoft.Extensions.Logging;
using Sessions.Application.Common.Interfaces;
using Sessions.Domain.Enums;

namespace Sessions.Application.Sessions.ValidateQr;

public class ValidateQrCommandHandler : IRequestHandler<ValidateQrCommand, ValidateQrResult>
{
    private readonly ISessionRepository _repository;
    private readonly IGameSessionFacade _facade;
    private readonly ILogger<ValidateQrCommandHandler> _logger;

    public ValidateQrCommandHandler(
        ISessionRepository repository,
        IGameSessionFacade facade,
        ILogger<ValidateQrCommandHandler> logger)
    {
        _repository = repository;
        _facade = facade;
        _logger = logger;
    }

    public async Task<ValidateQrResult> Handle(ValidateQrCommand command, CancellationToken ct)
    {
        var session = await _repository.GetByIdWithStagesAsync(command.SessionId, ct);

        if (session is null)
            return new ValidateQrResult(false, false, 0, 0, false, "Session not found");

        if (session.Status != SessionStatus.Active)
            return new ValidateQrResult(false, false, session.CurrentStageOrder,
                session.Stages.Count, false, $"Session is not active (status: {session.Status})");

        var currentStage = session.GetCurrentStage();
        if (currentStage is null)
            return new ValidateQrResult(false, false, session.CurrentStageOrder,
                session.Stages.Count, false, "No current stage found");

        if (!currentStage.ValidateQrToken(command.ScannedStageId, command.ScannedToken))
        {
            _logger.LogWarning(
                "QR validation failed: SessionId={SessionId}, ScannedStageId={ScannedStageId}",
                command.SessionId, command.ScannedStageId);
            return new ValidateQrResult(false, false, session.CurrentStageOrder,
                session.Stages.Count, false, "Invalid QR code for current stage");
        }

        var isLastStage = session.CurrentStageOrder >= session.Stages.Count - 1;

        if (isLastStage)
        {
            _logger.LogInformation(
                "QR validated on last stage: SessionId={SessionId}, StageOrder={Order}",
                command.SessionId, session.CurrentStageOrder);
            return new ValidateQrResult(true, false, session.CurrentStageOrder,
                session.Stages.Count, true);
        }

        session.AdvanceStage();
        await _repository.UpdateAsync(session, ct);
        await _facade.NotifyStageAdvanced(command.SessionId, session.CurrentStageOrder, ct);

        _logger.LogInformation(
            "QR validated and stage advanced: SessionId={SessionId}, NewOrder={Order}",
            command.SessionId, session.CurrentStageOrder);

        return new ValidateQrResult(true, true, session.CurrentStageOrder,
            session.Stages.Count, session.CurrentStageOrder >= session.Stages.Count - 1);
    }
}
