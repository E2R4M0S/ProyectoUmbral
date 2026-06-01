using Sessions.Domain.Enums;

namespace Sessions.Domain.States;

public static class StateFactory
{
    private static readonly Dictionary<SessionStatus, ISessionState> _states = new()
    {
        [SessionStatus.Scheduled] = new ScheduledState(),
        [SessionStatus.Preparing] = new PreparingState(),
        [SessionStatus.Active] = new ActiveState(),
        [SessionStatus.Paused] = new PausedState(),
        [SessionStatus.Finished] = new FinishedState(),
        [SessionStatus.Cancelled] = new CancelledState()
    };

    public static ISessionState Create(SessionStatus status) => _states[status];
}