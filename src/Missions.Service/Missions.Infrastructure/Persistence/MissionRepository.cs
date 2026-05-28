using Microsoft.EntityFrameworkCore;
using Missions.Application.Common.Interfaces;
using Missions.Domain.Entities;
using Missions.Domain.Enums;

namespace Missions.Infrastructure.Persistence;

public class MissionRepository : IMissionRepository
{
    private readonly MissionsDbContext _context;

    public MissionRepository(MissionsDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Mission mission, CancellationToken ct)
    {
        await _context.Missions.AddAsync(mission, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<bool> IsTitleUniqueAsync(string title, CancellationToken ct)
    {
        return !await _context.Missions.AnyAsync(m => m.Title == title, ct);
    }

    public async Task<(IReadOnlyList<Mission> Missions, int TotalCount)> GetMissionsAsync(
        string? search, string? difficulty, string? status,
        int page, int pageSize, CancellationToken ct)
    {
        var query = _context.Missions.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLowerInvariant();
            query = query.Where(m => m.Title.ToLower().Contains(searchLower));
        }

        if (!string.IsNullOrWhiteSpace(difficulty) && Enum.TryParse<Difficulty>(difficulty, ignoreCase: true, out var diff))
        {
            query = query.Where(m => m.Difficulty == diff);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<MissionStatus>(status, ignoreCase: true, out var stat))
        {
            query = query.Where(m => m.Status == stat);
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<Mission?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _context.Missions.FindAsync([id], ct);
    }
}