namespace Trivia.Application.Common.Interfaces;

public interface IParticipantAnswerRepository
{
    Task AddAsync(Trivia.Domain.Entities.ParticipantAnswer answer, CancellationToken ct = default);
    Task<List<Trivia.Domain.Entities.ParticipantAnswer>> GetByQuizAsync(Guid quizId, CancellationToken ct = default);
    Task<int> GetCountByQuestionAsync(Guid questionId, CancellationToken ct = default);
}
