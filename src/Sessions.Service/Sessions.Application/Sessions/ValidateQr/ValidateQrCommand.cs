using MediatR;

namespace Sessions.Application.Sessions.ValidateQr;

public record ValidateQrCommand(
    Guid SessionId,
    Guid ScannedStageId,
    string ScannedToken
) : IRequest<ValidateQrResult>;

public record ValidateQrResult(
    bool IsValid,
    bool Advanced,
    int CurrentStageOrder,
    int TotalStages,
    bool IsLastStage,
    string? ErrorMessage = null
);
