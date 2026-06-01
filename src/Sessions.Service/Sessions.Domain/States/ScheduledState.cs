using Sessions.Domain.Entities;
using Sessions.Domain.Enums;

namespace Sessions.Domain.States;

public class ScheduledState : ISessionState
{
    public SessionStatus Status => SessionStatus.Scheduled;

    private static readonly HashSet<SessionStatus> Allowed = new()
    {
        SessionStatus.Preparing,
        SessionStatus.Cancelled
    };

    public bool CanTransitionTo(SessionStatus target) => Allowed.Contains(target);

    public void OnEnter(Session context) { }
    public void OnExit(Session context) { }
}