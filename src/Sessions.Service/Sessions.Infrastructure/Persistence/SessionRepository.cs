using Microsoft.EntityFrameworkCore;
using Sessions.Application.Common.Interfaces;
using Sessions.Domain.Entities;

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

    public async Task<bool> IsPinUniqueAsync(string pin, CancellationToken ct)
    {
        return !await _context.Sessions
            .AnyAsync(s => s.Pin == pin, ct);
    }
}
