using Sessions.Domain.Entities;

namespace Sessions.Application.Sessions.Transition.Chain;

/// <summary>
/// Validates that the session is not already in a terminal state.
/// </summary>
public class NotTerminalHandler : BaseStateTransitionHandler
{
    protected override void Validate(Session session, string newStatus)
    {
        if (session.Status == Domain.Enums.SessionStatus.Finished ||
            session.Status == Domain.Enums.SessionStatus.Cancelled)
        {
            throw new InvalidOperationException(
                $"La sesión ya está en estado '{session.Status}' y no puede transicionar.");
        }
    }
}
