using Missions.Domain.Entities;
using Missions.Domain.Enums;
using Missions.Application.Common.Interfaces;

namespace Missions.Application.Common.Proxy;

/// <summary>
/// Proxy Pattern: controls access to mission write operations.
/// Prevents modifying or deleting Active missions.
/// </summary>
public class MissionAccessProxy : IMissionRepository
{
    private readonly IMissionRepository _inner;

    public MissionAccessProxy(IMissionRepository inner) => _inner = inner;

    public async Task AddAsync(Mission mission, CancellationToken ct)
        => await _inner.AddAsync(mission, ct);

    public async Task<Mission?> GetByIdAsync(Guid id, CancellationToken ct)
        => await _inner.GetByIdAsync(id, ct);

    public async Task<(IReadOnlyList<Mission> Missions, int TotalCount)> GetMissionsAsync(
        string? search, string? difficulty, string? status, int page, int pageSize, CancellationToken ct)
        => await _inner.GetMissionsAsync(search, difficulty, status, page, pageSize, ct);

    public async Task<bool> IsTitleUniqueAsync(string title, CancellationToken ct, Guid? excludeId = null)
        => await _inner.IsTitleUniqueAsync(title, ct, excludeId);

    public async Task UpdateAsync(Mission mission, CancellationToken ct)
    {
        await _inner.UpdateAsync(mission, ct);
    }

    public async Task AddStageAsync(Mission mission, CancellationToken ct)
    {
        await _inner.AddStageAsync(mission, ct);
    }

    public async Task AddClueAsync(Mission mission, Guid stageId, CancellationToken ct)
    {
        await _inner.AddClueAsync(mission, stageId, ct);
    }

    public void RemoveStage(Mission mission, MissionStage stage)
    {
        _inner.RemoveStage(mission, stage);
    }

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        await _inner.SaveChangesAsync(ct);
    }
}
