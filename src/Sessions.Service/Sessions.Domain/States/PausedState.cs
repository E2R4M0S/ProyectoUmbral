using Sessions.Domain.Entities;
using Sessions.Domain.Enums;

namespace Sessions.Domain.States;

public class PausedState : ISessionState
{
    public SessionStatus Status => SessionStatus.Paused;

    private static readonly HashSet<SessionStatus> Allowed = new()
    {
        SessionStatus.Active,
        SessionStatus.Finished,
        SessionStatus.Cancelled
    };

    public bool CanTransitionTo(SessionStatus target) => Allowed.Contains(target);

    public void OnEnter(Session context)
    {
        context.PausedAt = DateTime.UtcNow;
    }

    public void OnExit(Session context) { }
}