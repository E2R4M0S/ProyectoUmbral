using MediatR;
using Missions.Application.Missions.Clues;

namespace Missions.Application.Missions.Clues;

public record DeleteClueCommand(
    Guid MissionId,
    Guid StageId,
    Guid ClueId
) : IRequest<bool>;
