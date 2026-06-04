using Microsoft.EntityFrameworkCore;
using Trivia.Application.Common.Interfaces;
using Trivia.Domain.Entities;

namespace Trivia.Infrastructure.Persistence;

public class QuizRepository : IQuizRepository
{
    private readonly TriviaDbContext _db;

    public QuizRepository(TriviaDbContext db) => _db = db;

    public async Task<Quiz?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Set<Quiz>().FirstOrDefaultAsync(q => q.Id == id, ct);
    }

    public async Task<List<Quiz>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Set<Quiz>().ToListAsync(ct);
    }

    public async Task AddAsync(Quiz quiz, CancellationToken ct = default)
    {
        await _db.Set<Quiz>().AddAsync(quiz, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<Question>> GetQuestionsAsync(Guid quizId, CancellationToken ct = default)
    {
        return await _db.Set<Question>().Where(q => q.QuizId == quizId).ToListAsync(ct);
    }

    public async Task<List<Answer>> GetAnswersAsync(Guid questionId, CancellationToken ct = default)
    {
        return await _db.Set<Answer>().Where(a => a.QuestionId == questionId).ToListAsync(ct);
    }
}
