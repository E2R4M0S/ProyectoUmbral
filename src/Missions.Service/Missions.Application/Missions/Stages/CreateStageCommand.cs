using MediatR;

namespace Missions.Application.Missions.Stages;

public record CreateStageCommand(
    Guid MissionId,
    string Name,
    string Description,
    int Order
) : IRequest<CreateStageCommandResult>;
