using MediatR;
using Microsoft.EntityFrameworkCore;
using Trivia.Application.Quizzes.Commands;

namespace Trivia.Infrastructure.Handlers;

public class DeleteQuizHandler : IRequestHandler<DeleteQuizCommand, MediatR.Unit>
{
    private readonly TriviaDbContext _db;
    public DeleteQuizHandler(TriviaDbContext db) => _db = db;

    public async Task<Unit> Handle(DeleteQuizCommand request, CancellationToken cancellationToken)
    {
        var quiz = await _db.Quizzes!.Include(q => q.Questions).FirstOrDefaultAsync(q => q.Id == request.QuizId, cancellationToken);
        if (quiz is null) throw new KeyNotFoundException("Quiz not found");

        // check for active sessions would happen here (not implemented)

        _db.Quizzes!.Remove(quiz);
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
