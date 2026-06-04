using Trivia.Domain.Entities;

namespace Trivia.Application.Common.Interfaces;

public interface IAnswerRepository
{
    Task<List<Answer>> GetByQuestionIdAsync(Guid questionId, CancellationToken ct = default);
    Task AddRangeAsync(List<Answer> answers, CancellationToken ct = default);
}
