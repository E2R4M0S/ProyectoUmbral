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

    // Pulls a lagging participant forward to the session's current stage (e.g. when the
    // session advances past a Trivia stage, which has no QR for the participant to scan).
    public void CatchUpTo(int order)
    {
        if (order > CurrentStageOrder) CurrentStageOrder = order;
    }

    public void Complete() => CompletedAt = DateTime.UtcNow;

    public bool HasCompleted => CompletedAt.HasValue;

    public void SetWaiting() => IsWaitingAtGate = true;

    public void ClearWaiting() => IsWaitingAtGate = false;

    public int Score { get; private set; }

    // RB-08: ranking tie-break — when the score last changed, so ties can be broken by
    // whichever participant reached that score first.
    public DateTime? LastScoreAt { get; private set; }

    public void AddScore(int delta) { if (delta > 0) { Score += delta; LastScoreAt = DateTime.UtcNow; } }

    public void ApplyPenalty(int amount) { if (amount > 0) { Score = Math.Max(0, Score - amount); LastScoreAt = DateTime.UtcNow; } }

    public void ResetScore() { Score = 0; LastScoreAt = null; }
}