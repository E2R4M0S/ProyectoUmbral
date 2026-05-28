using Microsoft.EntityFrameworkCore;
using Missions.Application.Common.Interfaces;
using Missions.Domain.Entities;

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
}