namespace Sessions.Application.Sessions.Consult;

public record SessionListItemDto(
    Guid Id,
    string Name,
    Guid MissionId,
    string Status,
    DateTime? StartedAt,
    DateTime? EndedAt,
    DateTime CreatedAt
);