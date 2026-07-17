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
            return;
        }

        // Resuming from Paused: shift the mission clock forward by however long we were
        // paused, so elapsed-time math (and RF-02 timeout enforcement) picks up right where
        // it left off instead of counting the paused interval against the mission's budget.
        if (context.PausedAt.HasValue)
        {
            var pausedDuration = DateTime.UtcNow - context.PausedAt.Value;
            context.TotalPausedSeconds += pausedDuration.TotalSeconds;
            if (context.CurrentMissionStartedAt.HasValue)
            {
                context.CurrentMissionStartedAt = context.CurrentMissionStartedAt.Value.Add(pausedDuration);
            }
            context.PausedAt = null;
        }
    }

    public void OnExit(Session context) { }
}