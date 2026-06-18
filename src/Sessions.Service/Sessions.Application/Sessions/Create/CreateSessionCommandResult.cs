namespace Sessions.Application.Sessions.Create;

public record CreateSessionCommandResult(
    Guid Id,
    string Name,
    string Pin,
    string Status,
    int CurrentStageOrder,
    IReadOnlyList<StageOutput> Stages,
    DateTime? StartedAt,
    DateTime? EndedAt,
    DateTime CreatedAt);

public record StageOutput(
    Guid MissionId,
    string MissionTitle,
    string MissionType,
    int Order);
