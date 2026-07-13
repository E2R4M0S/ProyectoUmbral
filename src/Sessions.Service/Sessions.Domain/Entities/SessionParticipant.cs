namespace Sessions.Domain.Entities;

public class SessionParticipant
{
    public Guid Id { get; private set; }
    public Guid SessionId { get; private set; }
    public Guid UserId { get; private set; }
    public string UserAlias { get; private set; } = null!;
    public DateTime JoinedAt { get; private set; }
    public int CurrentStageOrder { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private SessionParticipant() { }

    public static SessionParticipant Create(Guid sessionId, Guid userId, string? userAlias = null)
    {
        return new SessionParticipant
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            UserId = userId,
            UserAlias = userAlias ?? userId.ToString("N")[..8],
            JoinedAt = DateTime.UtcNow,
            CurrentStageOrder = 0,
        };
    }

    public bool IsWaitingAtGate { get; private set; }

    public void AdvanceStage() => CurrentStageOrder++;

    public void Complete() => CompletedAt = DateTime.UtcNow;

    public bool HasCompleted => CompletedAt.HasValue;

    public void SetWaiting() => IsWaitingAtGate = true;

    public void ClearWaiting() => IsWaitingAtGate = false;

    public int Score { get; private set; }

    public void AddScore(int delta) { if (delta > 0) Score += delta; }
}