using MediatR;
using Microsoft.EntityFrameworkCore;
using Trivia.Application.Quizzes.Queries;

namespace Trivia.Infrastructure.Handlers;

public class GetQuizByIdHandler : IRequestHandler<GetQuizByIdQuery, object?>
{
    private readonly TriviaDbContext _db;
    public GetQuizByIdHandler(TriviaDbContext db) => _db = db;

    public async Task<object?> Handle(GetQuizByIdQuery request, CancellationToken cancellationToken)
    {
        var quiz = await _db.Quizzes!.Include(q => q.Questions!).ThenInclude(q => q.Answers).FirstOrDefaultAsync(q => q.Id == request.QuizId, cancellationToken);
        if (quiz is null) return null;
        return new
        {
            quiz.Id,
            quiz.Name,
            quiz.Description,
            Questions = quiz.Questions.Select(q => new
            {
                q.Id,
                q.Text,
                q.TimeLimitSeconds,
                q.Order,
                Answers = q.Answers.Select(a => new { a.Id, a.Text, a.IsCorrect })
            })
        };
    }
}
