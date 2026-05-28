namespace Missions.Application.Missions.Stages;

public record UpdateStageCommandResult(
    Guid Id,
    string Name,
    string Description,
    int Order
);
