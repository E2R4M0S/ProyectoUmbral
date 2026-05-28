using Sessions.Domain.Entities;

namespace Sessions.Application.Common.Interfaces;

public interface ISessionRepository
{
    Task AddAsync(Session session, CancellationToken ct);
    Task<bool> IsPinUniqueAsync(string pin, CancellationToken ct);
}
