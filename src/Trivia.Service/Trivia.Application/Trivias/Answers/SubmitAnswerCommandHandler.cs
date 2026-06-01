using MediatR;
using Trivia.Application.Common.Interfaces;

namespace Trivia.Application.Trivias.Answers;

public class SubmitAnswerCommandHandler : IRequestHandler<SubmitAnswerCommand>
{
    private readonly Trivia.Application.Common.Interfaces.IEventPublisher _publisher;
<<<<<<< HEAD
    private readonly Trivia.Application.Common.Interfaces.IParticipantAnswerRepository? _answerRepo;
    private readonly ILogger<SubmitAnswerCommandHandler> _logger;

    public SubmitAnswerCommandHandler(IEventPublisher publisher, ILogger<SubmitAnswerCommandHandler> logger, Trivia.Application.Common.Interfaces.IParticipantAnswerRepository? answerRepo = null)
    {
        _publisher = publisher;
        _logger = logger;
        _answerRepo = answerRepo;
=======
    private readonly ILogger<SubmitAnswerCommandHandler> _logger;

    public SubmitAnswerCommandHandler(IEventPublisher publisher, ILogger<SubmitAnswerCommandHandler> logger)
    {
        _publisher = publisher;
        _logger = logger;
>>>>>>> 61b3cec (feat(hu-39): countdown timer frontend + backend endpoints and skeleton for answer submission and RabbitMQ consumer)
    }

    public async Task Handle(SubmitAnswerCommand request, CancellationToken ct)
    {
<<<<<<< HEAD
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
=======
        // Minimal implementation: publish an integration event containing the answer details.
        // Persistent storage (ParticipantAnswer table) and correctness check should be handled by a consumer
        // subscribed to this event (as required by HU-41).
        var payload = new
        {
            request.QuizId,
            request.TeamId,
            request.QuestionId,
            request.AnswerId,
            request.Timestamp
>>>>>>> 61b3cec (feat(hu-39): countdown timer frontend + backend endpoints and skeleton for answer submission and RabbitMQ consumer)
        };

        await _publisher.PublishAsync("TriviaAnswerSubmittedEvent", payload, ct);
    }
}
