using MediatR;

namespace Sessions.Application.Sessions.Create;

public record CreateSessionCommand(
    string Name,
    List<StageInput> Stages) : IRequest<CreateSessionCommandResult>;

public record StageInput(
    Guid MissionId,
    string MissionTitle,
    string MissionType,
    int Order);
