using Missions.Application.Missions.Stages;

namespace Missions.Application.Missions.Catalog;

public record MissionDetailDto(
    Guid Id,
    string Title,
    string Description,
    string Difficulty,
    int TimeMinutes,
    string Type,
    string Status,
    IReadOnlyList<StageDto> Stages
);
