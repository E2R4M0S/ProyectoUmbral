namespace Sessions.Domain.Entities;

public class SessionTeamMember
{
    public Guid Id { get; private set; }
    public Guid SessionTeamId { get; private set; }
    public Guid UserId { get; private set; }
    public string UserAlias { get; private set; } = null!;
    public DateTime JoinedAt { get; private set; }

    private SessionTeamMember() { }

    public static SessionTeamMember Create(Guid sessionTeamId, Guid userId, string? userAlias)
    {
        return new SessionTeamMember
        {
            Id = Guid.NewGuid(),
            SessionTeamId = sessionTeamId,
            UserId = userId,
            UserAlias = string.IsNullOrWhiteSpace(userAlias) ? userId.ToString("N")[..8] : userAlias.Trim(),
            JoinedAt = DateTime.UtcNow
        };
    }
}
