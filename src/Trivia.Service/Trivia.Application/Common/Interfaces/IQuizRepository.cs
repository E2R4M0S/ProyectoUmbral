namespace Trivia.Application.Common.Interfaces;

public interface IQuizRepository
{
    Task<Trivia.Domain.Entities.Quiz?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
