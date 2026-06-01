namespace Trivia.Application.Common.Interfaces;

public interface IParticipantAnswerRepository
{
    Task AddAsync(Trivia.Domain.Entities.ParticipantAnswer answer, CancellationToken ct = default);
}
