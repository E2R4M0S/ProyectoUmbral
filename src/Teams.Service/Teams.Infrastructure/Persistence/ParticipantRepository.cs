using Microsoft.EntityFrameworkCore;
using Teams.Application.Common.Interfaces;
using Teams.Domain.Entities;

namespace Teams.Infrastructure.Persistence;

public class ParticipantRepository : IParticipantRepository
{
    private readonly TeamsDbContext _context;

    public ParticipantRepository(TeamsDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Participant participant, CancellationToken ct)
    {
        await _context.Participants.AddAsync(participant, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<bool> IsAliasUniqueAsync(string alias, CancellationToken ct)
    {
        return !await _context.Participants.AnyAsync(p => p.Alias == alias, ct);
    }

    public async Task<bool> IsEmailUniqueAsync(string email, CancellationToken ct)
    {
        return !await _context.Participants.AnyAsync(p => p.Email == email, ct);
    }

    public async Task<Participant?> GetByKeycloakUserIdAsync(string keycloakUserId, CancellationToken ct)
    {
        return await _context.Participants
            .FirstOrDefaultAsync(p => p.KeycloakUserId == keycloakUserId, ct);
    }

    public async Task UpdateAsync(Participant participant, CancellationToken ct)
    {
        _context.Participants.Update(participant);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<bool> IsAliasUniqueAsync(string alias, string? excludeKeycloakUserId, CancellationToken ct)
    {
        return !await _context.Participants
            .AnyAsync(p => p.Alias == alias && p.KeycloakUserId != excludeKeycloakUserId, ct);
    }
}
