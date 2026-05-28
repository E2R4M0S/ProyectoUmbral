namespace Missions.Application.Missions.Stages;

public record CreateStageCommandResult(
    Guid Id,
    string Name,
    string Description,
    int Order
);
