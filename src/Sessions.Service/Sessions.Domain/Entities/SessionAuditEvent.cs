namespace Sessions.Domain.Entities;

public static class SessionAuditEventTypes
{
    public const string PenaltyApplied = "PenaltyApplied";
    public const string EvidenceValidated = "EvidenceValidated";
    public const string EvidenceRejected = "EvidenceRejected";
    public const string StatusChanged = "StatusChanged";
    public const string TriviaAnswerScored = "TriviaAnswerScored";
    public const string AutomaticClueReleased = "AutomaticClueReleased";
    // RB-04: marks a predefined clue as released to a specific recipient (team/user/everyone)
    // for a stage, so ReleaseClueEndpoint can refuse to release the same clue to them twice.
    public const string ManualClueReleased = "ManualClueReleased";
}

public class SessionAuditEvent
{
    public Guid Id { get; private set; }
    public Guid SessionId { get; private set; }
    public string EventType { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public Guid? TeamId { get; private set; }
    public Guid? UserId { get; private set; }
    // RB-05: lets an event be tied to the specific clue it concerns (e.g. de-duplicating
    // automatic clue releases) — not populated for events that aren't clue-related.
    public Guid? ClueId { get; private set; }
    public int? ScoreDelta { get; private set; }
    public DateTime OccurredAt { get; private set; }

    private SessionAuditEvent() { }

    public static SessionAuditEvent Create(
        Guid sessionId,
        string eventType,
        string description,
        Guid? teamId = null,
        Guid? userId = null,
        int? scoreDelta = null,
        Guid? clueId = null)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            throw new InvalidOperationException("SessionAuditEvent EventType is required");
        if (string.IsNullOrWhiteSpace(description))
            throw new InvalidOperationException("SessionAuditEvent Description is required");

        return new SessionAuditEvent
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            EventType = eventType,
            Description = description.Trim(),
            TeamId = teamId,
            UserId = userId,
            ScoreDelta = scoreDelta,
            ClueId = clueId,
            OccurredAt = DateTime.UtcNow
        };
    }
}
