using MediatR;
using Microsoft.EntityFrameworkCore;
using Trivia.Application.Questions.Commands;

namespace Trivia.Infrastructure.Handlers;

public class DeleteQuestionHandler : IRequestHandler<DeleteQuestionCommand, MediatR.Unit>
{
    private readonly TriviaDbContext _db;
    public DeleteQuestionHandler(TriviaDbContext db) => _db = db;

    public async Task<Unit> Handle(DeleteQuestionCommand request, CancellationToken cancellationToken)
    {
        var question = await _db.Questions!.Include(q => q.Answers).FirstOrDefaultAsync(q => q.Id == request.QuestionId && q.QuizId == request.QuizId, cancellationToken);
        if (question is null) throw new KeyNotFoundException("Question not found");

        _db.Questions!.Remove(question);
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
