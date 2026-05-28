using Missions.Application.Common.Interfaces;

namespace Missions.Application.Common.Services;

public class PassthroughMissionStageValidator : IMissionStageValidator
{
    public Task<bool> HasAtLeastOneStageAsync(Guid missionId, CancellationToken ct)
        => Task.FromResult(true);
}