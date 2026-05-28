using MediatR;
using Missions.Application.Missions.Clues;

namespace Missions.Application.Missions.Clues;

public record CreateClueCommand(
    Guid MissionId,
    Guid StageId,
    string Content,
    int? Penalty,
    string ReleaseType
) : IRequest<CreateClueCommandResult>;
