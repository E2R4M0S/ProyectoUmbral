using Missions.Domain.Entities;
using Missions.Domain.Enums;

namespace Missions.Application.Missions.StatusChange.Chain;

/// <summary>
/// Validates that activating a Draft mission requires every stage to have at least one clue.
/// </summary>
public class EachStageRequiresClueHandler : BaseMissionStatusHandler
{
    protected override void Validate(Mission mission, string newStatus)
    {
        var target = Enum.Parse<MissionStatus>(newStatus, ignoreCase: true);

        if (mission.Status == MissionStatus.Draft && target == MissionStatus.Active)
        {
            if (mission.Stages.Any(s => s.Clues.Count == 0))
            {
                throw new InvalidOperationException(
                    "No se puede activar una misión con etapas sin pistas. Agregue al menos una pista a cada etapa.");
            }
        }
    }
}
