using Missions.Domain.Entities;
using Missions.Domain.Enums;

namespace Missions.Application.Missions.StatusChange.Chain;

/// <summary>
/// Validates that the status string is a valid MissionStatus value.
/// </summary>
public class ValidMissionStatusHandler : BaseMissionStatusHandler
{
    protected override void Validate(Mission mission, string newStatus)
    {
        if (!Enum.TryParse<MissionStatus>(newStatus, ignoreCase: true, out _))
        {
            throw new InvalidOperationException(
                $"'{newStatus}' no es un estado válido. " +
                $"Estados permitidos: {string.Join(", ", Enum.GetNames<MissionStatus>())}");
        }
    }
}
