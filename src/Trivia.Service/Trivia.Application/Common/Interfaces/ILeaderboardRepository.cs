using Trivia.Domain.Entities;

namespace Trivia.Application.Common.Interfaces;

public interface ILeaderboardRepository
{
    Task<LeaderboardEntry?> GetByTeamAsync(Guid quizId, Guid teamId, CancellationToken ct = default);
    Task AddOrUpdateAsync(LeaderboardEntry entry, CancellationToken ct = default);
    Task<List<LeaderboardEntry>> GetByQuizAsync(Guid quizId, CancellationToken ct = default);
    Task<List<LeaderboardEntry>> GetGlobalAsync(DateTime? since = null, CancellationToken ct = default);
}
