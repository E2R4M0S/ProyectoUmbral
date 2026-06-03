using MediatR;
using Microsoft.EntityFrameworkCore;
using Trivia.Application.Answers.Commands;

namespace Trivia.Infrastructure.Handlers;

public class AddAnswerHandler : IRequestHandler<AddAnswerCommand, Guid>
{
    private readonly TriviaDbContext _db;
    public AddAnswerHandler(TriviaDbContext db) => _db = db;

    public async Task<Guid> Handle(AddAnswerCommand request, CancellationToken cancellationToken)
    {
        var question = await _db.Questions!.Include(q => q.Answers).FirstOrDefaultAsync(q => q.Id == request.QuestionId && q.QuizId == request.QuizId, cancellationToken);
        if (question is null) throw new KeyNotFoundException("Question not found");

        if (question.Answers.Count >= 4) throw new InvalidOperationException("Maximum answers reached");
        if (string.IsNullOrWhiteSpace(request.Text)) throw new ArgumentException("Answer text required");

        var answer = new Trivia.Domain.Answer { QuestionId = request.QuestionId, Text = request.Text };
        _db.Answers!.Add(answer);
        await _db.SaveChangesAsync(cancellationToken);
        return answer.Id;
    }
}
