using Sessions.Domain.Entities;
using Sessions.Domain.Enums;

namespace Sessions.Domain.States;

public interface ISessionState
{
    SessionStatus Status { get; }
    bool CanTransitionTo(SessionStatus target);
    void OnEnter(Session context);
    void OnExit(Session context);
}