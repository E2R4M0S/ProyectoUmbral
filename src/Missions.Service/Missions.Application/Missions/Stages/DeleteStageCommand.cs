using MediatR;

namespace Missions.Application.Missions.Stages;

public record DeleteStageCommand(Guid MissionId, Guid StageId) : IRequest;
