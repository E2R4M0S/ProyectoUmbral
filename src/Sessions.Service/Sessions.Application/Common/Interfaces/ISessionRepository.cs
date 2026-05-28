using Sessions.Domain.Entities;

namespace Sessions.Application.Common.Interfaces;

public interface ISessionRepository
{
    Task AddAsync(Session session, CancellationToken ct);
    Task<bool> IsPinUniqueAsync(string pin, CancellationToken ct);
    Task<(IReadOnlyList<Session> Sessions, int TotalCount)> GetSessionsAsync(
        string? search,
        string? status,
        Guid? missionId,
        int page,
        int pageSize,
        CancellationToken ct);
}
