using Missions.Domain.Entities;

namespace Missions.Application.Common.Interfaces;

public interface IMissionRepository
{
    Task AddAsync(Mission mission, CancellationToken ct);
    Task<bool> IsTitleUniqueAsync(string title, CancellationToken ct);
}