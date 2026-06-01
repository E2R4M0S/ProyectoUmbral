using MediatR;
using Trivia.Application.Common.Interfaces;

namespace Trivia.Application.Trivias.Answers;

public class SubmitAnswerCommandHandler : IRequestHandler<SubmitAnswerCommand>
{
    private readonly Trivia.Application.Common.Interfaces.IEventPublisher _publisher;
    private readonly Trivia.Application.Common.Interfaces.IParticipantAnswerRepository? _answerRepo;
    private readonly ILogger<SubmitAnswerCommandHandler> _logger;

    public SubmitAnswerCommandHandler(IEventPublisher publisher, ILogger<SubmitAnswerCommandHandler> logger, Trivia.Application.Common.Interfaces.IParticipantAnswerRepository? answerRepo = null)
    {
        _publisher = publisher;
        _logger = logger;
        _answerRepo = answerRepo;
    }

    public async Task Handle(SubmitAnswerCommand request, CancellationToken ct)
    {
        // Persist the participant answer locally
        var answer = new Trivia.Domain.Entities.ParticipantAnswer
        {
            Id = Guid.NewGuid(),
            QuizId = request.QuizId,
            TeamId = request.TeamId,
            QuestionId = request.QuestionId,
            AnswerId = request.AnswerId,
            Timestamp = request.Timestamp,
            IsCorrect = false // correctness check will be determined below
        };

        // Basic correctness check: try to map answer against quiz data (simplified)
        // For now, we won't load the full question/answer model; assume correctness is checked elsewhere.

        // Save to DB
        if (_answerRepo is not null)
        {
            await _answerRepo.AddAsync(answer, ct);
        }

        // Publish integration event for other services (leaderboard consumer)
        var payload = new
        {
            answer.Id,
            answer.QuizId,
            answer.TeamId,
            answer.QuestionId,
            answer.AnswerId,
            answer.Timestamp
        };

        await _publisher.PublishAsync("TriviaAnswerSubmittedEvent", payload, ct);
    }
}
