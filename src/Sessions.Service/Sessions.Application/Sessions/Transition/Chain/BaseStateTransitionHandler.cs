using Sessions.Domain.Entities;

namespace Sessions.Application.Sessions.Transition.Chain;

/// <summary>
/// Base class for the Chain of Responsibility. Implements SetNext()
/// and provides a template for Handle().
/// </summary>
public abstract class BaseStateTransitionHandler : IStateTransitionHandler
{
    private IStateTransitionHandler? _next;

    public IStateTransitionHandler SetNext(IStateTransitionHandler handler)
    {
        _next = handler;
        return handler;
    }

    public void Handle(Session session, string newStatus)
    {
        Validate(session, newStatus);
        _next?.Handle(session, newStatus);
    }

    protected abstract void Validate(Session session, string newStatus);
}
