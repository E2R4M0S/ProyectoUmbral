using Sessions.Domain.Entities;

namespace Sessions.Application.Sessions.Transition.Chain;

/// <summary>
/// Chain of Responsibility: each handler validates one aspect of a session state transition.
/// If valid, passes to the next handler. If invalid, throws.
/// </summary>
public interface IStateTransitionHandler
{
    IStateTransitionHandler SetNext(IStateTransitionHandler handler);
    void Handle(Session session, string newStatus);
}
