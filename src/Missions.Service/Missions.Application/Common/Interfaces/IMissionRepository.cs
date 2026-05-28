using Missions.Domain.Entities;

namespace Missions.Application.Common.Interfaces;

public interface IMissionRepository
{
    Task AddAsync(Mission mission, CancellationToken ct);
    Task<bool> IsTitleUniqueAsync(string title, CancellationToken ct);
    Task<(IReadOnlyList<Mission> Missions, int TotalCount)> GetMissionsAsync(
        string? search, string? difficulty, string? status,
        int page, int pageSize, CancellationToken ct);
    Task<Mission?> GetByIdAsync(Guid id, CancellationToken ct);
}