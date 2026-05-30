using Sessions.Domain.Enums;

namespace Sessions.Domain.Entities;

public class Session
{
    private static readonly Dictionary<SessionStatus, HashSet<SessionStatus>> ValidTransitions = new()
    {
        [SessionStatus.Scheduled] = new() { SessionStatus.Preparing, SessionStatus.Cancelled },
        [SessionStatus.Preparing] = new() { SessionStatus.Active, SessionStatus.Cancelled },
        [SessionStatus.Active] = new() { SessionStatus.Paused, SessionStatus.Finished, SessionStatus.Cancelled },
        [SessionStatus.Paused] = new() { SessionStatus.Active, SessionStatus.Finished, SessionStatus.Cancelled },
        [SessionStatus.Finished] = new() { },
        [SessionStatus.Cancelled] = new() { }
    };

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public Guid MissionId { get; private set; }
    public string MissionTitle { get; private set; } = null!;
    public string Pin { get; private set; } = null!;
    public SessionStatus Status { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? EndedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private readonly List<SessionParticipant> _participants = new();
    public IReadOnlyList<SessionParticipant> Participants => _participants.AsReadOnly();

    public static Session Create(string name, Guid missionId, string missionTitle, string pin)
    {
        return new Session
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            MissionId = missionId,
            MissionTitle = missionTitle.Trim(),
            Pin = pin,
            Status = SessionStatus.Scheduled,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void TransitionTo(SessionStatus newStatus)
    {
        if (Status == newStatus)
        {
            throw new InvalidOperationException($"Session is already in '{Status}' status");
        }

        if (!ValidTransitions.TryGetValue(Status, out var allowed) || !allowed.Contains(newStatus))
        {
            throw new InvalidOperationException(
                $"Cannot transition session from '{Status}' to '{newStatus}'");
        }

        if (newStatus == SessionStatus.Active && !StartedAt.HasValue)
        {
            StartedAt = DateTime.UtcNow;
        }

        if (newStatus == SessionStatus.Finished || newStatus == SessionStatus.Cancelled)
        {
            EndedAt = DateTime.UtcNow;
        }

        Status = newStatus;
    }

    public void AddParticipant(Guid userId)
    {
        if (Status != SessionStatus.Preparing)
        {
            throw new InvalidOperationException(
                $"Cannot join session in '{Status}' status");
        }

        if (_participants.Any(p => p.UserId == userId))
        {
            throw new InvalidOperationException("User has already joined this session");
        }

        _participants.Add(SessionParticipant.Create(Id, userId));
    }
}
