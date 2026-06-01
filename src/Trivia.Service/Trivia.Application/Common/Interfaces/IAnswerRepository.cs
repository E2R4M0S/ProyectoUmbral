using Trivia.Domain.Entities;

namespace Trivia.Application.Common.Interfaces;

public interface IAnswerRepository
{
    Task<List<Trivia.Domain.Entities.Answer>> GetByQuestionIdAsync(Guid questionId, CancellationToken ct = default);
}
