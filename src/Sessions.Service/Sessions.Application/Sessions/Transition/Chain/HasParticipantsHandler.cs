using Sessions.Domain.Entities;
using Sessions.Domain.Enums;

namespace Sessions.Application.Sessions.Transition.Chain;

/// <summary>
/// Validates that the session has at least one participant before activating.
/// </summary>
public class HasParticipantsHandler : BaseStateTransitionHandler
{
    protected override void Validate(Session session, string newStatus)
    {
        if (Enum.TryParse<SessionStatus>(newStatus, ignoreCase: true, out var target) &&
            target == SessionStatus.Active &&
            session.Participants.Count == 0)
        {
            throw new InvalidOperationException(
                "No se puede activar la sesión sin participantes. Al menos un participante debe haberse unido.");
        }
    }
}
