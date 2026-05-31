using MediatR;
using Microsoft.EntityFrameworkCore;
using Trivia.Application.Questions.Commands;

namespace Trivia.Infrastructure.Handlers;

public class EditQuestionHandler : IRequestHandler<EditQuestionCommand, MediatR.Unit>
{
    private readonly TriviaDbContext _db;
    public EditQuestionHandler(TriviaDbContext db) => _db = db;

    public async Task<Unit> Handle(EditQuestionCommand request, CancellationToken cancellationToken)
    {
        var question = await _db.Questions!.FirstOrDefaultAsync(q => q.Id == request.QuestionId && q.QuizId == request.QuizId, cancellationToken);
        if (question is null) throw new KeyNotFoundException("Question not found");

        // validate order uniqueness
        if (await _db.Questions!.AnyAsync(q => q.QuizId == request.QuizId && q.Order == request.Order && q.Id != request.QuestionId, cancellationToken))
            throw new InvalidOperationException("Question order already exists");

        question.Text = request.Text;
        question.TimeLimitSeconds = request.TimeLimitSeconds;
        question.Order = request.Order;

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
