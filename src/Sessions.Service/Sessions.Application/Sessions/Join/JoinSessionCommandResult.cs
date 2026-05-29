namespace Sessions.Application.Sessions.Join;

public record JoinSessionCommandResult(Guid SessionId, Guid UserId, DateTime JoinedAt);