using Sessions.Domain.Entities;

namespace Sessions.Application.Common.Interfaces;

public interface ISessionRepository
{
    Task AddAsync(Session session, CancellationToken ct);
    Task<Session?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Session?> GetByIdWithStagesAsync(Guid id, CancellationToken ct);
    Task<Session?> GetByPinAsync(string pin, CancellationToken ct);
    Task<Session?> GetByNameAsync(string name, CancellationToken ct);
    Task<bool> IsPinUniqueAsync(string pin, CancellationToken ct);
    Task<(IReadOnlyList<Session> Sessions, int TotalCount)> GetSessionsAsync(
        string? search,
        string? status,
        Guid? missionId,
        int page,
        int pageSize,
        CancellationToken ct);
    Task UpdateAsync(Session session, CancellationToken ct);
    Task AddParticipantAsync(SessionParticipant participant, CancellationToken ct);
    Task<SessionParticipant?> GetParticipantAsync(Guid sessionId, Guid userId, CancellationToken ct);
    Task UpdateParticipantAsync(SessionParticipant participant, CancellationToken ct);
    Task AddParticipantScoreAsync(Guid sessionId, Guid userId, int delta, CancellationToken ct = default);
    Task<List<(Guid UserId, string Alias, int TotalScore)>> GetGlobalParticipantRankingAsync(DateTime? since = null, CancellationToken ct = default);
    Task ResetParticipantScoresAsync(Guid sessionId, CancellationToken ct = default);

    // Teams
    Task AddTeamAsync(SessionTeam team, CancellationToken ct);
    Task<SessionTeam?> GetTeamByIdAsync(Guid teamId, CancellationToken ct);
    Task<List<SessionTeam>> GetTeamsBySessionIdAsync(Guid sessionId, CancellationToken ct);
    Task<bool> IsTeamNameUniqueInSessionAsync(Guid sessionId, string name, CancellationToken ct);
    Task<SessionTeam?> GetParticipantTeamInSessionAsync(Guid sessionId, Guid userId, CancellationToken ct);
    Task UpdateTeamAsync(SessionTeam team, CancellationToken ct);
    Task AddTeamScoreAsync(Guid teamId, int delta, CancellationToken ct = default);
    Task<List<SessionRankingEntry>> GetSessionRankingAsync(Guid sessionId, CancellationToken ct = default);

    // Clue penalties
    // teamId null = the clue was broadcast to the whole session (current UI behavior): every
    // team and every teamless participant currently in the session is penalized.
    Task ApplyCluePenaltyAsync(Guid sessionId, Guid? teamId, int amount, CancellationToken ct = default);
}

public record SessionRankingEntry(
    string Type,        // "team" | "individual"
    string DisplayName,
    int Score,
    int MemberCount,    // 0 for individual
    Guid? TeamId,
    Guid? UserId
);
