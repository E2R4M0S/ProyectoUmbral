using Microsoft.EntityFrameworkCore;
using Trivia.Application.Common.Interfaces;
using Trivia.Domain.Entities;

namespace Trivia.Infrastructure.Persistence;

public class LeaderboardRepository : ILeaderboardRepository
{
    private readonly TriviaDbContext _db;

    public LeaderboardRepository(TriviaDbContext db)
    {
        _db = db;
    }

    public async Task<LeaderboardEntry?> GetByTeamAsync(Guid quizId, Guid teamId, CancellationToken ct = default)
    {
        return await _db.Set<LeaderboardEntry>().FirstOrDefaultAsync(e => e.QuizId == quizId && e.TeamId == teamId, ct);
    }

    public async Task AddOrUpdateAsync(LeaderboardEntry entry, CancellationToken ct = default)
    {
        var existing = await GetByTeamAsync(entry.QuizId, entry.TeamId, ct);
        if (existing is null)
        {
            entry.Id = Guid.NewGuid();
            entry.UpdatedAt = DateTime.UtcNow;
            await _db.Set<LeaderboardEntry>().AddAsync(entry, ct);
        }
        else
        {
            existing.Score = entry.Score;
            existing.UpdatedAt = DateTime.UtcNow;
            _db.Set<LeaderboardEntry>().Update(existing);
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<LeaderboardEntry>> GetByQuizAsync(Guid quizId, CancellationToken ct = default)
    {
        return await _db.Set<LeaderboardEntry>().Where(e => e.QuizId == quizId).OrderByDescending(e => e.Score).ToListAsync(ct);
    }

    public async Task<List<LeaderboardEntry>> GetGlobalAsync(DateTime? since = null, CancellationToken ct = default)
    {
        var query = _db.Set<LeaderboardEntry>().AsQueryable();
        if (since.HasValue)
            query = query.Where(e => e.UpdatedAt >= since.Value);

        var all = await query.ToListAsync(ct);

        return all
            .GroupBy(e => new { e.TeamId, e.TeamName })
            .Select(g => new LeaderboardEntry
            {
                Id = Guid.Empty,
                QuizId = Guid.Empty,
                TeamId = g.Key.TeamId,
                TeamName = g.Key.TeamName,
                Score = g.Sum(e => e.Score),
                UpdatedAt = g.Max(e => e.UpdatedAt),
            })
            .OrderByDescending(e => e.Score)
            .ToList();
    }
}
