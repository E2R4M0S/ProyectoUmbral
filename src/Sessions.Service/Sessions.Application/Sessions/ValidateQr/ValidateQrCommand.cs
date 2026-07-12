using MediatR;

namespace Sessions.Application.Sessions.ValidateQr;

public record ValidateQrCommand(
    Guid SessionId,
    Guid UserId,
    Guid ScannedStageId,
    string ScannedToken
) : IRequest<ValidateQrResult>;

public record ValidateQrResult(
    bool IsValid,
    bool Advanced,
    int CurrentStageOrder,
    int TotalStages,
    bool IsLastStage,
    bool IsAtGate = false,
    bool GateOpened = false,
    int GatePosition = 0,
    int GateThreshold = 0,
    bool IsEliminated = false,
    string? ErrorMessage = null
);
