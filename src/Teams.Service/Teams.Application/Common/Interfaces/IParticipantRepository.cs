using Teams.Domain.Entities;

namespace Teams.Application.Common.Interfaces;

public interface IParticipantRepository
{
    Task AddAsync(Participant participant, CancellationToken ct);
    Task<bool> IsAliasUniqueAsync(string alias, CancellationToken ct);
    Task<bool> IsEmailUniqueAsync(string email, CancellationToken ct);
}
