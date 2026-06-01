using MediatR;
using Microsoft.Extensions.Logging;
using Trivia.Application.Common.Interfaces;
using Trivia.Domain.Entities;

namespace Trivia.Application.Trivias.StartTrivia;

public class StartTriviaCommandHandler : IRequestHandler<StartTriviaCommand>
{
    private readonly IQuizRepository _repo; // assume repository exists in domain layer
    private readonly IEventPublisher _publisher;
    private readonly ILogger<StartTriviaCommandHandler> _logger;

    public StartTriviaCommandHandler(IQuizRepository repo, IEventPublisher publisher, ILogger<StartTriviaCommandHandler> logger)
    {
        _repo = repo;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task Handle(StartTriviaCommand request, CancellationToken ct)
    {
        // Minimal flow: validate quiz exists and publish a TriviaStarted event
        var quiz = await _repo.GetByIdAsync(request.QuizId, ct);
        if (quiz is null) throw new InvalidOperationException($"Quiz with id '{request.QuizId}' not found");

        _logger.LogInformation("Starting trivia for QuizId={QuizId}", request.QuizId);

        await _publisher.PublishAsync("TriviaStarted", new { QuizId = request.QuizId, StartedAt = DateTime.UtcNow }, ct);
    }
}
