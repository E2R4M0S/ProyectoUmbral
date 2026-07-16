namespace Sessions.Application.Sessions.Consult;

public record SessionProgressDto(
    Guid SessionId,
    string Name,
    string Status,
    int ElapsedSeconds,
    int TotalDurationSeconds,
    int CurrentMissionElapsedSeconds,
    IReadOnlyList<ParticipantProgressDto> Participants);

public record ParticipantProgressDto(
    Guid UserId,
    string UserAlias,
    DateTime JoinedAt);
