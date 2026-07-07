using MediatR;

namespace Missions.Application.Missions.Stages;

public record CreateStageCommand(
    Guid MissionId,
    string Name,
    string Description,
    int Order,
    double? Latitude = null,
    double? Longitude = null
) : IRequest<CreateStageCommandResult>;
