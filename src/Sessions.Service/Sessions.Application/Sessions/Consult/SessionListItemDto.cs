namespace Sessions.Application.Sessions.Consult;

public record SessionListItemDto(
    Guid Id,
    string Name,
    string MissionTitle,
    string MissionType,
    int CurrentStageOrder,
    int StageCount,
    string Pin,
    int ParticipantCount,
    string Status,
    DateTime? StartedAt,
    DateTime? EndedAt,
    DateTime CreatedAt
);
