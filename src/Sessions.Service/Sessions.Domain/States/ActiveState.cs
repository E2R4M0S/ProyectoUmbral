using Sessions.Domain.Entities;
using Sessions.Domain.Enums;

namespace Sessions.Domain.States;

public class ActiveState : ISessionState
{
    public SessionStatus Status => SessionStatus.Active;

    private static readonly HashSet<SessionStatus> Allowed = new()
    {
        SessionStatus.Paused,
        SessionStatus.Finished,
        SessionStatus.Cancelled
    };

    public bool CanTransitionTo(SessionStatus target) => Allowed.Contains(target);

    public void OnEnter(Session context)
    {
        if (context.StartedAt is null)
        {
            context.StartedAt = DateTime.UtcNow;
            context.CurrentMissionStartedAt = context.StartedAt;
        }
    }

    public void OnExit(Session context) { }
}