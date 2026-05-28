using Sessions.Application.Sessions.Create;

namespace Sessions.Application.Sessions.Create;

public record CreateSessionCommandResult(
    Guid Id,
    string Name,
    Guid MissionId,
    string Pin,
    string Status,
    DateTime? StartedAt,
    DateTime? EndedAt,
    DateTime CreatedAt);
