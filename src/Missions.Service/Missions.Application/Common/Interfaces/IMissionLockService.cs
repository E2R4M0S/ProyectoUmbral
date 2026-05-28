namespace Missions.Application.Common.Interfaces;

public interface IMissionLockService
{
    Task<bool> IsMissionInUseAsync(Guid missionId, CancellationToken ct);
}
