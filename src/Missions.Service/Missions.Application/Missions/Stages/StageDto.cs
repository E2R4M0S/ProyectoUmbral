using Missions.Application.Missions.Clues;
using Missions.Application.Missions.Stages;

namespace Missions.Application.Missions.Catalog;

public record StageDto(
    Guid Id,
    string Name,
    string Description,
    int Order,
    IReadOnlyList<ClueDto> Clues
);
