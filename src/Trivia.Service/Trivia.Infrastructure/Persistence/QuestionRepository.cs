using Microsoft.EntityFrameworkCore;
using Trivia.Application.Common.Interfaces;
using Trivia.Domain.Entities;

namespace Trivia.Infrastructure.Persistence;

public class QuestionRepository : IQuestionRepository
{
    private readonly TriviaDbContext _db;

    public QuestionRepository(TriviaDbContext db) => _db = db;

    public async Task AddRangeAsync(List<Question> questions, CancellationToken ct = default)
    {
        await _db.Set<Question>().AddRangeAsync(questions, ct);
        await _db.SaveChangesAsync(ct);
    }
}
