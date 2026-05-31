using MediatR;
using Microsoft.EntityFrameworkCore;
using Trivia.Application.Quizzes.Queries;

namespace Trivia.Infrastructure.Handlers;

public class GetQuizzesHandler : IRequestHandler<GetQuizzesQuery, IEnumerable<object>>
{
    private readonly TriviaDbContext _db;
    public GetQuizzesHandler(TriviaDbContext db) => _db = db;

    public async Task<IEnumerable<object>> Handle(GetQuizzesQuery request, CancellationToken cancellationToken)
    {
        var q = _db.Quizzes!.AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.Q)) q = q.Where(x => x.Name.Contains(request.Q));
        var skip = (request.Page - 1) * request.Size;
        var list = await q.OrderBy(x => x.Name).Skip(skip).Take(request.Size)
            .Select(x => new { x.Id, x.Name, x.Description, QuestionsCount = x.Questions.Count })
            .ToListAsync(cancellationToken);
        return list;
    }
}
