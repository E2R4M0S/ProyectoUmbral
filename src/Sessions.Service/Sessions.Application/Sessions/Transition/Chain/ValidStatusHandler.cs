using Sessions.Domain.Entities;
using Sessions.Domain.Enums;

namespace Sessions.Application.Sessions.Transition.Chain;

/// <summary>
/// Validates that the new status string can be parsed to a valid SessionStatus enum value.
/// </summary>
public class ValidStatusHandler : BaseStateTransitionHandler
{
    protected override void Validate(Session session, string newStatus)
    {
        if (!Enum.TryParse<SessionStatus>(newStatus, ignoreCase: true, out _))
        {
            throw new InvalidOperationException(
                $"'{newStatus}' no es un estado válido. " +
                $"Estados permitidos: {string.Join(", ", Enum.GetNames<SessionStatus>())}");
        }
    }
}
