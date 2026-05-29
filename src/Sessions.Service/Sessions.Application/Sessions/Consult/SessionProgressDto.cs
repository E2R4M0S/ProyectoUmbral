namespace Sessions.Application.Sessions.Consult;

public record SessionProgressDto(
    Guid SessionId,
    string Name,
    string Status,
    DateTime? StartedAt,
    DateTime? EndedAt,
    IReadOnlyList<ParticipantProgressDto> Participants);

public record ParticipantProgressDto(
    Guid UserId,
    DateTime JoinedAt);
