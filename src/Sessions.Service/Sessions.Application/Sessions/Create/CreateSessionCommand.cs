using MediatR;

namespace Sessions.Application.Sessions.Create;

public record CreateSessionCommand(
    string Name,
    List<StageInput> Stages) : IRequest<CreateSessionCommandResult>;

public record StageInput(
    Guid MissionId,
    Guid MissionStageId,
    string MissionTitle,
    string MissionType,
    int Order,
    string QrToken,
    int TimeMinutes = 0,
    double? Latitude = null,
    double? Longitude = null);
