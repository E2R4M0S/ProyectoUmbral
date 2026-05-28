using Teams.Domain.Entities;

namespace Teams.Application.Common.Interfaces;

public interface ITeamRepository
{
    Task AddAsync(Team team, CancellationToken ct);
    Task UpdateAsync(Team team, CancellationToken ct);
    Task<bool> IsNameUniqueAsync(string name, CancellationToken ct);
    Task<(IReadOnlyList<Team> Teams, int TotalCount)> GetTeamsAsync(
        string? search, int page, int pageSize, CancellationToken ct);
    Task<Team?> GetByIdWithMembersAsync(Guid id, CancellationToken ct);
}
