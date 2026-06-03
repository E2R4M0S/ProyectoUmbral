using MediatR;
using Microsoft.EntityFrameworkCore;
using Trivia.Application.Quizzes.Commands;

namespace Trivia.Infrastructure.Handlers;

public class CreateQuizHandler : IRequestHandler<CreateQuizCommand, Guid>
{
    private readonly TriviaDbContext _db;

    public CreateQuizHandler(TriviaDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateQuizCommand request, CancellationToken cancellationToken)
    {
        if (await _db.Quizzes!.AnyAsync(x => x.Name == request.Name, cancellationToken))
            throw new InvalidOperationException("Quiz name already exists");

        var quiz = new Trivia.Domain.Quiz { Name = request.Name, Description = request.Description };
        _db.Quizzes!.Add(quiz);
        await _db.SaveChangesAsync(cancellationToken);
        return quiz.Id;
    }
}
