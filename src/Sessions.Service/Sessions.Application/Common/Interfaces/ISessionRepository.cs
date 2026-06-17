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
}
