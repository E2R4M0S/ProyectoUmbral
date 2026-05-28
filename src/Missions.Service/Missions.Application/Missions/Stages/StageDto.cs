namespace Missions.Application.Missions.Stages;

public record StageDto(
    Guid Id,
    string Name,
    string Description,
    int Order
);
