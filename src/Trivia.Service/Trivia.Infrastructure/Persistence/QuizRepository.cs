using Microsoft.EntityFrameworkCore;
using Trivia.Application.Common.Interfaces;
using Trivia.Domain.Entities;

namespace Trivia.Infrastructure.Persistence;

public class QuizRepository : IQuizRepository
{
    private readonly TriviaDbContext _db;

    public QuizRepository(TriviaDbContext db)
    {
        _db = db;
    }

    public async Task<Quiz?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Set<Quiz>().FirstOrDefaultAsync(q => q.Id == id, ct);
    }
}
