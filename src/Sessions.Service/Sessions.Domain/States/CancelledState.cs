using Sessions.Domain.Entities;
using Sessions.Domain.Enums;

namespace Sessions.Domain.States;

public class CancelledState : ISessionState
{
    public SessionStatus Status => SessionStatus.Cancelled;

    private static readonly HashSet<SessionStatus> Allowed = new();

    public bool CanTransitionTo(SessionStatus target) => false;

    public void OnEnter(Session context)
    {
        context.EndedAt = DateTime.UtcNow;
    }

    public void OnExit(Session context) { }
}