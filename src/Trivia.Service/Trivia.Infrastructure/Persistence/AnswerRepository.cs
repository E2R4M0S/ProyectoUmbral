using Microsoft.EntityFrameworkCore;
using Trivia.Application.Common.Interfaces;
using Trivia.Domain.Entities;

namespace Trivia.Infrastructure.Persistence;

public class AnswerRepository : IAnswerRepository
{
    private readonly TriviaDbContext _db;

    public AnswerRepository(TriviaDbContext db)
    {
        _db = db;
    }

    public async Task<List<Answer>> GetByQuestionIdAsync(Guid questionId, CancellationToken ct = default)
    {
        return await _db.Set<Answer>().Where(a => a.QuestionId == questionId).ToListAsync(ct);
    }
}
