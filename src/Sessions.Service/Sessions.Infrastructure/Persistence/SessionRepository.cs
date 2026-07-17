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
        var query = _context.Sessions.Include(s => s.Participants).AsQueryable();

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

    public async Task<SessionParticipant?> GetParticipantAsync(Guid sessionId, Guid userId, CancellationToken ct)
    {
        return await _context.Set<SessionParticipant>()
            .FirstOrDefaultAsync(p => p.SessionId == sessionId && p.UserId == userId, ct);
    }

    public async Task UpdateParticipantAsync(SessionParticipant participant, CancellationToken ct)
    {
        _context.Set<SessionParticipant>().Update(participant);
        await _context.SaveChangesAsync(ct);
    }

    public async Task AddParticipantScoreAsync(Guid sessionId, Guid userId, int delta, CancellationToken ct = default)
    {
        var participant = await GetParticipantAsync(sessionId, userId, ct);
        if (participant is null) return;
        participant.AddScore(delta);
        _context.Set<SessionParticipant>().Update(participant);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<List<Session>> GetSessionsForParticipantAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.Sessions
            .Include(s => s.Participants)
            .Where(s => s.Participants.Any(p => p.UserId == userId))
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task ResetParticipantScoresAsync(Guid sessionId, CancellationToken ct = default)
    {
        var participants = await _context.Set<SessionParticipant>()
            .Where(p => p.SessionId == sessionId)
            .ToListAsync(ct);
        foreach (var p in participants)
            p.ResetScore();

        // Team scores accrue alongside member scores (AddTeamScoreAsync) and must be wiped
        // the same way, or a cancelled session would leave stale team points behind.
        var teams = await _context.SessionTeams
            .Where(t => t.SessionId == sessionId)
            .ToListAsync(ct);
        foreach (var t in teams)
            t.ResetScore();

        await _context.SaveChangesAsync(ct);
    }

    public async Task AddTeamAsync(SessionTeam team, CancellationToken ct)
    {
        await _context.SessionTeams.AddAsync(team, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<SessionTeam?> GetTeamByIdAsync(Guid teamId, CancellationToken ct)
    {
        return await _context.SessionTeams
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.Id == teamId, ct);
    }

    public async Task<List<SessionTeam>> GetTeamsBySessionIdAsync(Guid sessionId, CancellationToken ct)
    {
        return await _context.SessionTeams
            .Include(t => t.Members)
            .Where(t => t.SessionId == sessionId)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<bool> IsTeamNameUniqueInSessionAsync(Guid sessionId, string name, CancellationToken ct)
    {
        return !await _context.SessionTeams
            .AnyAsync(t => t.SessionId == sessionId && t.Name.ToLower() == name.ToLower().Trim(), ct);
    }

    public async Task<SessionTeam?> GetParticipantTeamInSessionAsync(Guid sessionId, Guid userId, CancellationToken ct)
    {
        return await _context.SessionTeams
            .Include(t => t.Members)
            .Where(t => t.SessionId == sessionId)
            .FirstOrDefaultAsync(t => t.Members.Any(m => m.UserId == userId), ct);
    }

    public async Task UpdateTeamAsync(SessionTeam team, CancellationToken ct)
    {
        // EF Core does not automatically detect additions to private List<T> backing fields.
        // Compare current members with DB members and explicitly Add/Remove.
        var dbMemberIds = (await _context.Set<SessionTeamMember>()
            .Where(m => m.SessionTeamId == team.Id)
            .Select(m => m.Id)
            .ToListAsync(ct))
            .ToHashSet();

        foreach (var member in team.Members)
        {
            if (!dbMemberIds.Contains(member.Id))
                _context.Set<SessionTeamMember>().Add(member);
        }

        foreach (var dbId in dbMemberIds)
        {
            if (!team.Members.Any(m => m.Id == dbId))
            {
                var orphan = await _context.Set<SessionTeamMember>().FindAsync(new object[] { dbId }, ct);
                if (orphan is not null)
                    _context.Set<SessionTeamMember>().Remove(orphan);
            }
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task AddTeamScoreAsync(Guid teamId, int delta, CancellationToken ct = default)
    {
        if (delta <= 0) return;

        var team = await _context.SessionTeams
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.Id == teamId, ct);
        if (team is null) return;

        team.AddScore(delta);

        // Distribute the same points to every member's individual score
        foreach (var member in team.Members)
        {
            var participant = await GetParticipantAsync(team.SessionId, member.UserId, ct);
            if (participant is null) continue;
            participant.AddScore(delta);
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task ApplyCluePenaltyAsync(Guid sessionId, Guid? teamId, int amount, string? reason = null, CancellationToken ct = default)
    {
        if (amount <= 0) return;

        var description = string.IsNullOrWhiteSpace(reason) ? "Penalización por pista liberada" : reason.Trim();

        if (teamId is { } tid)
        {
            var team = await _context.SessionTeams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.Id == tid, ct);
            if (team is null) return;

            team.ApplyPenalty(amount);
            foreach (var member in team.Members)
            {
                var participant = await GetParticipantAsync(team.SessionId, member.UserId, ct);
                participant?.ApplyPenalty(amount);
            }

            await _context.Set<SessionAuditEvent>().AddAsync(
                SessionAuditEvent.Create(sessionId, SessionAuditEventTypes.PenaltyApplied, description, teamId: tid, scoreDelta: -amount), ct);

            await _context.SaveChangesAsync(ct);
            return;
        }

        // No specific team: the clue was broadcast to everyone in the session, so every team
        // and every teamless participant pays the penalty.
        var teams = await _context.SessionTeams
            .Include(t => t.Members)
            .Where(t => t.SessionId == sessionId)
            .ToListAsync(ct);

        foreach (var team in teams)
        {
            team.ApplyPenalty(amount);
            foreach (var member in team.Members)
            {
                var participant = await GetParticipantAsync(sessionId, member.UserId, ct);
                participant?.ApplyPenalty(amount);
            }
        }

        var membersInTeams = teams.SelectMany(t => t.Members).Select(m => m.UserId).ToHashSet();
        var soloParticipants = await _context.Set<SessionParticipant>()
            .Where(p => p.SessionId == sessionId && !membersInTeams.Contains(p.UserId))
            .ToListAsync(ct);
        foreach (var p in soloParticipants)
            p.ApplyPenalty(amount);

        await _context.Set<SessionAuditEvent>().AddAsync(
            SessionAuditEvent.Create(sessionId, SessionAuditEventTypes.PenaltyApplied, description, teamId: null, scoreDelta: -amount), ct);

        await _context.SaveChangesAsync(ct);
    }

    public async Task AddAuditEventAsync(SessionAuditEvent auditEvent, CancellationToken ct = default)
    {
        await _context.Set<SessionAuditEvent>().AddAsync(auditEvent, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<List<SessionAuditEvent>> GetAuditTrailAsync(Guid sessionId, CancellationToken ct = default)
    {
        return await _context.Set<SessionAuditEvent>()
            .Where(e => e.SessionId == sessionId)
            .OrderByDescending(e => e.OccurredAt)
            .ToListAsync(ct);
    }

    public async Task<List<SessionRankingEntry>> GetSessionRankingAsync(Guid sessionId, CancellationToken ct = default)
    {
        // Live session ranking shows the team's name once for every member sharing it, and each
        // solo (teamless) participant by their own alias. The persistent/global ranking is a
        // separate query (GetGlobalParticipantRankingAsync) and always stays per-alias.
        var teams = await _context.SessionTeams
            .Include(t => t.Members)
            .Where(t => t.SessionId == sessionId)
            .ToListAsync(ct);

        var participants = await _context.Set<SessionParticipant>()
            .Where(p => p.SessionId == sessionId)
            .ToListAsync(ct);

        var membersInTeams = teams
            .SelectMany(t => t.Members)
            .Select(m => m.UserId)
            .ToHashSet();

        var entries = new List<SessionRankingEntry>();

        foreach (var team in teams)
        {
            entries.Add(new SessionRankingEntry(
                Type: "team",
                DisplayName: team.Name,
                Score: team.Score,
                MemberCount: team.Members.Count,
                TeamId: team.Id,
                UserId: null,
                LastScoreAt: team.LastScoreAt
            ));
        }

        foreach (var p in participants.Where(p => !membersInTeams.Contains(p.UserId)))
        {
            entries.Add(new SessionRankingEntry(
                Type: "individual",
                DisplayName: p.UserAlias,
                Score: p.Score,
                MemberCount: 0,
                TeamId: null,
                UserId: p.UserId,
                LastScoreAt: p.LastScoreAt
            ));
        }

        // RB-08: descending by score; ties broken by whoever reached that score first.
        return entries
            .OrderByDescending(e => e.Score)
            .ThenBy(e => e.LastScoreAt ?? DateTime.MaxValue)
            .ToList();
    }

    public async Task<List<(Guid UserId, string Alias, int TotalScore)>> GetGlobalParticipantRankingAsync(DateTime? since = null, CancellationToken ct = default)
    {
        // Only sessions that actually finished count toward the historical/global ranking — an
        // Active session's points are still provisional (could still be Cancelled, which wipes
        // them), so counting them here would let in-progress or later-discarded points leak
        // into a persistent, cross-session record.
        var query =
            from p in _context.Set<SessionParticipant>()
            join s in _context.Sessions on p.SessionId equals s.Id
            where s.Status == SessionStatus.Finished
            select p;

        if (since.HasValue)
            query = query.Where(p => p.JoinedAt >= since.Value);

        var all = await query.ToListAsync(ct);

        return all
            .GroupBy(p => p.UserId)
            .Select(g => (
                UserId: g.Key,
                Alias: g.First().UserAlias,
                TotalScore: g.Sum(p => p.Score)
            ))
            .Where(r => r.TotalScore > 0)
            .OrderByDescending(r => r.TotalScore)
            .ToList();
    }
}
