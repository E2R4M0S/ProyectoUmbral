using Missions.Application.Common.Interfaces;

namespace Missions.Application.Common.Services;

public class MissionStageValidator : IMissionStageValidator
{
    private readonly IMissionRepository _repository;

    public MissionStageValidator(IMissionRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> HasAtLeastOneStageAsync(Guid missionId, CancellationToken ct)
    {
        var mission = await _repository.GetByIdAsync(missionId, ct);
        return mission?.Stages.Any() ?? false;
    }
}
