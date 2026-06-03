using System.ComponentModel.DataAnnotations.Schema;
using Sessions.Domain.Enums;
using Sessions.Domain.States;

namespace Sessions.Domain.Entities;

public class Session
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public Guid MissionId { get; private set; }
    public string MissionTitle { get; private set; } = null!;
    public string Pin { get; private set; } = null!;

    private SessionStatus _status;
    [NotMapped] private ISessionState _state = null!;

    public SessionStatus Status
    {
        get => _status;
        private set { _status = value; _state = StateFactory.Create(value); }
    }

    public DateTime? StartedAt { get; internal set; }
    public DateTime? EndedAt { get; internal set; }
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
        _state ??= StateFactory.Create(_status);
        if (_state.Status == newStatus)
        {
            throw new InvalidOperationException($"Session is already in '{newStatus}' status");
        }

        if (!_state.CanTransitionTo(newStatus))
        {
            throw new InvalidOperationException(
                $"Cannot transition session from '{_state.Status}' to '{newStatus}'");
        }

        _state.OnExit(this);
        Status = newStatus;
        _state.OnEnter(this);
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