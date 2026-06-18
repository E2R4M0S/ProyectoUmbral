using Microsoft.EntityFrameworkCore;
using Sessions.Application.Common.Interfaces;
using Sessions.Domain.Entities;
using Sessions.Domain.Enums;

namespace Sessions.Infrastructure.Persistence;

public class SessionRepository : ISessionRepository
{
    private readonly SessionsDbContext _context;

    public SessionRepository(SessionsDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Session session, CancellationToken ct)
    {
        await _context.Sessions.AddAsync(session, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<Session?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _context.Sessions
            .Include(s => s.Participants)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<Session?> GetByIdWithStagesAsync(Guid id, CancellationToken ct)
    {
        // Stages are owned and loaded as part of the Session aggregate via JSONB
        return await _context.Sessions
            .Include(s => s.Participants)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<Session?> GetByPinAsync(string pin, CancellationToken ct)
    {
        return await _context.Sessions.FirstOrDefaultAsync(s => s.Pin == pin, ct);
    }

    public async Task<Session?> GetByNameAsync(string name, CancellationToken ct)
    {
        return await _context.Sessions.FirstOrDefaultAsync(s => s.Name == name, ct);
    }

    public async Task<bool> IsPinUniqueAsync(string pin, CancellationToken ct)
    {
        return !await _context.Sessions
            .AnyAsync(s => s.Pin == pin, ct);
    }

    public async Task<(IReadOnlyList<Session> Sessions, int TotalCount)> GetSessionsAsync(
        string? search,
        string? status,
        Guid? missionId,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var query = _context.Sessions.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLowerInvariant();
            query = query.Where(s => s.Name.ToLower().Contains(searchLower));
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<SessionStatus>(status, ignoreCase: true, out var sessionStatus))
        {
            query = query.Where(s => s.Status == sessionStatus);
        }

        if (missionId.HasValue)
        {
            // The session contains the mission if any of its stages references it
            // (we filter post-fetch because Stages is a JSONB owned collection).
            var all = await query.ToListAsync(ct);
            var filtered = all.Where(s => s.Stages.Any(st => st.MissionId == missionId.Value)).ToList();
            var totalFiltered = filtered.Count;
            var paged = filtered
                .OrderByDescending(s => s.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            return (paged, totalFiltered);
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task UpdateAsync(Session session, CancellationToken ct)
    {
        await _context.SaveChangesAsync(ct);
    }

    public async Task AddParticipantAsync(SessionParticipant participant, CancellationToken ct)
    {
        await _context.Set<SessionParticipant>().AddAsync(participant, ct);
        await _context.SaveChangesAsync(ct);
    }
}
