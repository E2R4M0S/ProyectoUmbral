using Sessions.Domain.Entities;
using Sessions.Domain.Enums;

namespace Sessions.Domain.States;

public class PreparingState : ISessionState
{
    public SessionStatus Status => SessionStatus.Preparing;

    private static readonly HashSet<SessionStatus> Allowed = new()
    {
        SessionStatus.Active,
        SessionStatus.Cancelled
    };

    public bool CanTransitionTo(SessionStatus target) => Allowed.Contains(target);

    public void OnEnter(Session context) { }
    public void OnExit(Session context) { }
}