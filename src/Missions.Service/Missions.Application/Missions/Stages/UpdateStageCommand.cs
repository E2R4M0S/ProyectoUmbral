using MediatR;

namespace Missions.Application.Missions.Stages;

public record UpdateStageCommand(
    Guid MissionId,
    Guid StageId,
    string Name,
    string Description,
    int Order,
    double? Latitude = null,
    double? Longitude = null
) : IRequest<UpdateStageCommandResult>;
