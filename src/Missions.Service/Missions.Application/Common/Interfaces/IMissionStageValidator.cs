namespace Missions.Application.Common.Interfaces;

public interface IMissionStageValidator
{
    Task<bool> HasAtLeastOneStageAsync(Guid missionId, CancellationToken ct);
}