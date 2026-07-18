using Missions.Domain.Entities;
using Missions.Domain.Enums;

namespace Missions.Application.Missions.StatusChange.Chain;

/// <summary>
/// Validates that activating a Draft mission requires at least one stage.
/// Trivia missions are exempt — they can be activated without stages.
/// </summary>
public class DraftToActiveRequiresStagesHandler : BaseMissionStatusHandler
{
    protected override void Validate(Mission mission, string newStatus)
    {
        var target = Enum.Parse<MissionStatus>(newStatus, ignoreCase: true);

        if (mission.Status == MissionStatus.Draft && target == MissionStatus.Active)
        {
            // Trivia missions can be activated without stages (questions are created in the quiz flow)
            if (mission.Type == MissionType.Trivia) return;

            if (mission.Stages.Count == 0)
            {
                throw new InvalidOperationException(
                    "No se puede activar una misión sin etapas. Agregue al menos una etapa.");
            }
        }
    }
}
