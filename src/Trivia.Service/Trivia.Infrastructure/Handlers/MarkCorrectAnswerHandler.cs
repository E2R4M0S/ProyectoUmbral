using MediatR;
using Microsoft.EntityFrameworkCore;
using Trivia.Application.Answers.Commands;

namespace Trivia.Infrastructure.Handlers;

public class MarkCorrectAnswerHandler : IRequestHandler<MarkCorrectAnswerCommand, MediatR.Unit>
{
    private readonly TriviaDbContext _db;
    public MarkCorrectAnswerHandler(TriviaDbContext db) => _db = db;

    public async Task<Unit> Handle(MarkCorrectAnswerCommand request, CancellationToken cancellationToken)
    {
        var question = await _db.Questions!.Include(q => q.Answers).FirstOrDefaultAsync(q => q.Id == request.QuestionId && q.QuizId == request.QuizId, cancellationToken);
        if (question is null) throw new KeyNotFoundException("Question not found");

        var answer = question.Answers.FirstOrDefault(a => a.Id == request.AnswerId);
        if (answer is null) throw new KeyNotFoundException("Answer not found");

        foreach (var a in question.Answers) a.IsCorrect = false;
        answer.IsCorrect = true;

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
