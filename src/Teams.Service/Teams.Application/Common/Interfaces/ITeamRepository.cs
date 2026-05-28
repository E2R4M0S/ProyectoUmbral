using Teams.Domain.Entities;

namespace Teams.Application.Common.Interfaces;

public interface ITeamRepository
{
    Task AddAsync(Team team, CancellationToken ct);
    Task<bool> IsNameUniqueAsync(string name, CancellationToken ct);
}
