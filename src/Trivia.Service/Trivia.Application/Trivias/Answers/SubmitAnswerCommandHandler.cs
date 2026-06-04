using MediatR;
using Microsoft.Extensions.Logging;
using Trivia.Application.Common.Interfaces;

namespace Trivia.Application.Trivias.Answers;

public class SubmitAnswerCommandHandler : IRequestHandler<SubmitAnswerCommand>
{
    private readonly Trivia.Application.Common.Interfaces.IEventPublisher _publisher;
    private readonly Trivia.Application.Common.Interfaces.IParticipantAnswerRepository? _answerRepo;
    private readonly Trivia.Application.Common.Interfaces.IAnswerRepository? _answerRepoAnswers;
    private readonly Trivia.Application.Common.Interfaces.ILeaderboardRepository? _leaderboardRepo;
    private readonly ILogger<SubmitAnswerCommandHandler> _logger;

    public SubmitAnswerCommandHandler(IEventPublisher publisher, ILogger<SubmitAnswerCommandHandler> logger, Trivia.Application.Common.Interfaces.IParticipantAnswerRepository? answerRepo = null, Trivia.Application.Common.Interfaces.IAnswerRepository? answerRepoAnswers = null, Trivia.Application.Common.Interfaces.ILeaderboardRepository? leaderboardRepo = null)
    {
        _publisher = publisher;
        _logger = logger;
        _answerRepo = answerRepo;
        _answerRepoAnswers = answerRepoAnswers;
        _leaderboardRepo = leaderboardRepo;
    }

    public async Task Handle(SubmitAnswerCommand request, CancellationToken ct)
    {
        // Treat every submitted answer as correct (10 points) for the demo scoring
        bool isCorrect = true;
        var answer = new Trivia.Domain.Entities.ParticipantAnswer
        {
            Id = Guid.NewGuid(),
            QuizId = request.QuizId,
            TeamId = request.TeamId,
            QuestionId = request.QuestionId,
            AnswerId = request.AnswerId,
            Timestamp = request.Timestamp,
            IsCorrect = isCorrect
        };

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
            answer.Timestamp,
            answer.IsCorrect
        };

        await _publisher.PublishAsync("TriviaAnswerSubmittedEvent", payload, ct);

        // Also update leaderboard locally so operator UI can read it immediately when no broker is available
        try
        {
            if (_leaderboardRepo is not null)
            {
                var delta = 10;
                var existing = await _leaderboardRepo.GetByTeamAsync(request.QuizId, request.TeamId, ct);
                if (existing == null)
                {
                    var entry = new Trivia.Domain.Entities.LeaderboardEntry { QuizId = request.QuizId, TeamId = request.TeamId, Score = delta };
                    await _leaderboardRepo.AddOrUpdateAsync(entry, ct);
                }
                else
                {
                    existing.Score += delta;
                    await _leaderboardRepo.AddOrUpdateAsync(existing, ct);
                }

                // Publish immediate leaderboard snapshot so realtime hub can broadcast (works with HttpEventPublisher fallback)
                try
                {
                    var leaderboard = await _leaderboardRepo.GetByQuizAsync(request.QuizId, ct);
                    await _publisher.PublishAsync("LeaderboardUpdated", leaderboard, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to publish immediate LeaderboardUpdated event");
                }
            }
            else
            {
                _logger.LogInformation("Answer recorded and event published for AnswerId={AnswerId}", answer.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to do immediate leaderboard update");
        }
    }
}
