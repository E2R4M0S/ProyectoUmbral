namespace Sessions.Domain.Entities;

public class SessionParticipant
{
    public Guid Id { get; private set; }
    public Guid SessionId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime JoinedAt { get; private set; }

    private SessionParticipant() { } // EF Core

    public static SessionParticipant Create(Guid sessionId, Guid userId)
    {
        return new SessionParticipant
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            UserId = userId,
            JoinedAt = DateTime.UtcNow
        };
    }
}