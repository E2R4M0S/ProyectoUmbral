using System.ComponentModel.DataAnnotations.Schema;
using Sessions.Domain.Enums;
using Sessions.Domain.States;

namespace Sessions.Domain.Entities;

public class Session
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string Pin { get; private set; } = null!;

    private readonly List<SessionStage> _stages = new();
    public IReadOnlyList<SessionStage> Stages => _stages.AsReadOnly();

    public int CurrentStageOrder { get; private set; }

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

    public static Session Create(string name, string pin, List<SessionStage> stages)
    {
        if (stages is null || stages.Count == 0)
            throw new InvalidOperationException("Session must have at least one stage");

        var duplicates = stages.GroupBy(s => s.Order).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicates.Count > 0)
            throw new InvalidOperationException(
                $"Session stages contain duplicate Order values: {string.Join(",", duplicates)}");

        var sortedOrders = stages.Select(s => s.Order).OrderBy(o => o).ToList();
        for (int i = 0; i < sortedOrders.Count; i++)
        {
            if (sortedOrders[i] != i + 1)
                throw new InvalidOperationException(
                    $"Stage Order values must be sequential starting at 1; got {string.Join(",", sortedOrders)}");
        }

        var ordered = stages.OrderBy(s => s.Order).ToList();
        var session = new Session
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Pin = pin,
            CurrentStageOrder = 0,
            Status = SessionStatus.Scheduled,
            CreatedAt = DateTime.UtcNow
        };
        session._stages.AddRange(ordered);
        return session;
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

    public SessionStage? GetCurrentStage()
    {
        if (_stages.Count == 0) return null;
        var order = CurrentStageOrder + 1;
        return _stages.FirstOrDefault(s => s.Order == order);
    }

    public void AdvanceStage()
    {
        if (Status != SessionStatus.Active)
            throw new InvalidOperationException(
                $"Cannot advance stage: session is not active (current status '{Status}')");

        if (CurrentStageOrder + 1 >= _stages.Count)
            throw new InvalidOperationException(
                "Cannot advance stage: already on the last stage");

        CurrentStageOrder++;
    }
}
