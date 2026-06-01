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
}
