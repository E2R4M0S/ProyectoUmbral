namespace Sessions.Domain.Entities;

public class SessionParticipant
{
    public Guid Id { get; private set; }
    public Guid SessionId { get; private set; }
    public Guid UserId { get; private set; }
    public string UserAlias { get; private set; } = null!;
    public DateTime JoinedAt { get; private set; }

    private SessionParticipant() { }

    public static SessionParticipant Create(Guid sessionId, Guid userId, string? userAlias = null)
    {
        return new SessionParticipant
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            UserId = userId,
            UserAlias = userAlias ?? userId.ToString("N")[..8],
            JoinedAt = DateTime.UtcNow
        };
    }
}