using Trivia.Domain.Entities;

namespace Trivia.Application.Common.Interfaces;

public interface IQuestionRepository
{
    Task AddRangeAsync(List<Question> questions, CancellationToken ct = default);
    Task MarkReleasedAsync(Guid questionId, DateTime releasedAt, CancellationToken ct = default);
}
