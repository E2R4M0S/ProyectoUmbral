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

    public async Task<(IReadOnlyList<Team> Teams, int TotalCount)> GetTeamsAsync(
        string? search, int page, int pageSize, CancellationToken ct)
    {
        var query = _context.Teams.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLowerInvariant();
            query = query.Where(t => t.Name.ToLower().Contains(searchLower));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(t => t.Members)
            .ToListAsync(ct);

        return (items, totalCount);
    }
}
