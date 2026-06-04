namespace Trivia.Application.Common.Interfaces;

public interface IQuizRepository
{
    Task<Trivia.Domain.Entities.Quiz?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Trivia.Domain.Entities.Quiz>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Trivia.Domain.Entities.Quiz quiz, CancellationToken ct = default);
    Task<List<Trivia.Domain.Entities.Question>> GetQuestionsAsync(Guid quizId, CancellationToken ct = default);
    Task<List<Trivia.Domain.Entities.Answer>> GetAnswersAsync(Guid questionId, CancellationToken ct = default);
}
