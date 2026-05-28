using Missions.Application.Common.Interfaces;

namespace Missions.Application.Common.Services;

public class PassthroughMissionLockService : IMissionLockService
{
    public Task<bool> IsMissionInUseAsync(Guid missionId, CancellationToken ct)
        => Task.FromResult(false);
}
