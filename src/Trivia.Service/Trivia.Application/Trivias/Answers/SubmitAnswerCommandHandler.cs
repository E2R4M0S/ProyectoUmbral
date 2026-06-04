using System.Net.Http.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using Trivia.Application.Common.Interfaces;
using Trivia.Application.Common.Strategies;

namespace Trivia.Application.Trivias.Answers;

public class SubmitAnswerCommandHandler : IRequestHandler<SubmitAnswerCommand>
{
    private readonly IEventPublisher _publisher;
    private readonly IParticipantAnswerRepository? _answerRepo;
    private readonly ILeaderboardRepository? _leaderboardRepo;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IScoringStrategy _scoringStrategy;
    private readonly ILogger<SubmitAnswerCommandHandler> _logger;

    public SubmitAnswerCommandHandler(
        IEventPublisher publisher,
        ILogger<SubmitAnswerCommandHandler> logger,
        IHttpClientFactory httpClientFactory,
        IScoringStrategy scoringStrategy,
        IParticipantAnswerRepository? answerRepo = null,
        ILeaderboardRepository? leaderboardRepo = null)
    {
        _publisher = publisher;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _scoringStrategy = scoringStrategy;
        _answerRepo = answerRepo;
        _leaderboardRepo = leaderboardRepo;
    }

    public async Task Handle(SubmitAnswerCommand request, CancellationToken ct)
    {
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
                var timeElapsed = request.Timestamp - request.AskedAt;
                var delta = _scoringStrategy.CalculateScore(timeElapsed, request.TimeLimitSeconds);

                var existing = await _leaderboardRepo.GetByTeamAsync(request.QuizId, request.TeamId, ct);
                if (existing == null)
                {
                    var entry = new Trivia.Domain.Entities.LeaderboardEntry { QuizId = request.QuizId, TeamId = request.TeamId, TeamName = request.TeamName, Score = delta };
                    await _leaderboardRepo.AddOrUpdateAsync(entry, ct);
                }
                else
                {
                    existing.Score += delta;
                    existing.TeamName = request.TeamName;
                    await _leaderboardRepo.AddOrUpdateAsync(existing, ct);
                }

                // Publish immediate leaderboard snapshot so realtime hub can broadcast
                try
                {
                    var leaderboard = await _leaderboardRepo.GetByQuizAsync(request.QuizId, ct);
                    var client = _httpClientFactory.CreateClient("realTimeHub");
                    await client.PostAsJsonAsync("/internal/events/LeaderboardUpdated", leaderboard, ct);
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
