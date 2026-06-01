using MediatR;
using Trivia.Application.Common.Interfaces;

namespace Trivia.Application.Trivias.Answers;

public class SubmitAnswerCommandHandler : IRequestHandler<SubmitAnswerCommand>
{
    private readonly Trivia.Application.Common.Interfaces.IEventPublisher _publisher;
    private readonly Trivia.Application.Common.Interfaces.IParticipantAnswerRepository? _answerRepo;
    private readonly Trivia.Application.Common.Interfaces.IAnswerRepository? _answerRepoAnswers;
    private readonly ILogger<SubmitAnswerCommandHandler> _logger;

    public SubmitAnswerCommandHandler(IEventPublisher publisher, ILogger<SubmitAnswerCommandHandler> logger, Trivia.Application.Common.Interfaces.IParticipantAnswerRepository? answerRepo = null, Trivia.Application.Common.Interfaces.IAnswerRepository? answerRepoAnswers = null)
    {
        _publisher = publisher;
        _logger = logger;
        _answerRepo = answerRepo;
        _answerRepoAnswers = answerRepoAnswers;
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
            IsCorrect = false // will be computed below
        };

        // Basic correctness check: load answers for question and determine correctness
        bool isCorrect = false;

        try
        {
            if (_answerRepoAnswers is not null)
            {
                var answers = await _answerRepoAnswers.GetByQuestionIdAsync(request.QuestionId, ct);
                var matched = answers.FirstOrDefault(a => a.Id == request.AnswerId);
                if (matched is not null)
                    isCorrect = matched.IsCorrect;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to determine correctness for AnswerId={AnswerId}", request.AnswerId);
        }

        // Save to DB
        if (_answerRepo is not null)
        {
            answer.IsCorrect = isCorrect;
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
            answer.Timestamp,
            answer.IsCorrect
        };

        await _publisher.PublishAsync("TriviaAnswerSubmittedEvent", payload, ct);
    }
}
