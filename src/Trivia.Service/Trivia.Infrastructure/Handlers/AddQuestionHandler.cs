using MediatR;
using Microsoft.EntityFrameworkCore;
using Trivia.Application.Questions.Commands;

namespace Trivia.Infrastructure.Handlers;

public class AddQuestionHandler : IRequestHandler<AddQuestionCommand, Guid>
{
    private readonly TriviaDbContext _db;
    public AddQuestionHandler(TriviaDbContext db) => _db = db;

    public async Task<Guid> Handle(AddQuestionCommand request, CancellationToken cancellationToken)
    {
        var quiz = await _db.Quizzes!.Include(q => q.Questions).FirstOrDefaultAsync(q => q.Id == request.QuizId, cancellationToken);
        if (quiz is null) throw new KeyNotFoundException("Quiz not found");

        if (quiz.Questions.Any(q => q.Order == request.Order))
            throw new InvalidOperationException("Question order already exists");

        var question = new Trivia.Domain.Question { QuizId = request.QuizId, Text = request.Text, TimeLimitSeconds = request.TimeLimitSeconds, Order = request.Order };
        _db.Questions!.Add(question);
        await _db.SaveChangesAsync(cancellationToken);
        return question.Id;
    }
}
