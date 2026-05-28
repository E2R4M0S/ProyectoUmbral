using Microsoft.EntityFrameworkCore;
using Teams.Application.Common.Interfaces;
using Teams.Domain.Entities;

namespace Teams.Infrastructure.Persistence;

public class TeamRepository : ITeamRepository
{
    private readonly TeamsDbContext _context;

    public TeamRepository(TeamsDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Team team, CancellationToken ct)
    {
        await _context.Teams.AddAsync(team, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<bool> IsNameUniqueAsync(string name, CancellationToken ct)
    {
        return !await _context.Teams.AnyAsync(t => t.Name == name, ct);
    }
}
