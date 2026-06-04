using System.Net.Http.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using Trivia.Application.Common.Interfaces;
using Trivia.Application.Trivias.Questions;

namespace Trivia.Application.Trivias.Answers;

public class SubmitAnswerCommandHandler : IRequestHandler<SubmitAnswerCommand, AnswerResult>
{
    private readonly IEventPublisher _publisher;
    private readonly IParticipantAnswerRepository? _answerRepo;
    private readonly ILeaderboardRepository? _leaderboardRepo;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SubmitAnswerCommandHandler> _logger;

    public SubmitAnswerCommandHandler(
        IEventPublisher publisher,
        ILogger<SubmitAnswerCommandHandler> logger,
        IHttpClientFactory httpClientFactory,
        IParticipantAnswerRepository? answerRepo = null,
        ILeaderboardRepository? leaderboardRepo = null)
    {
        _publisher = publisher;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _answerRepo = answerRepo;
        _leaderboardRepo = leaderboardRepo;
    }

    public async Task<AnswerResult> Handle(SubmitAnswerCommand request, CancellationToken ct)
    {
        // Check if the answer is correct using the stored correct answer index
        var correctIndex = AskQuestionCommandHandler.CorrectAnswers.GetValueOrDefault(request.QuestionId, -1);
        var selectedIndex = int.TryParse(request.AnswerId.ToString()?.Last().ToString(), out var idx) ? idx : -1;
        bool isCorrect = correctIndex >= 0 && selectedIndex == correctIndex;

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

        // Publish integration event
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

        // Update leaderboard for all participants (correct = points, incorrect = 0)
        int position = 0;
        int delta = 0;

        try
        {
            if (_leaderboardRepo is not null)
            {
                if (isCorrect)
                {
                    // Base points + position bonus for correct answers
                    var timestamps = AskQuestionCommandHandler.CorrectAnswerTimestamps
                        .GetOrAdd(request.QuestionId, _ => new List<DateTime>());

                    lock (timestamps)
                    {
                        timestamps.Add(request.Timestamp);
                        timestamps.Sort();
                        position = timestamps.IndexOf(request.Timestamp) + 1;
                    }

                    var bonus = position switch { 1 => 30, 2 => 20, 3 => 10, _ => 5 };
                    delta = 10 + bonus;
                }

                // Create or update leaderboard entry (even for 0 points, so everyone appears)
                var existing = await _leaderboardRepo.GetByTeamAsync(request.QuizId, request.TeamId, ct);
                if (existing == null)
                {
                    var entry = new Trivia.Domain.Entities.LeaderboardEntry
                    {
                        QuizId = request.QuizId,
                        TeamId = request.TeamId,
                        TeamName = request.TeamName,
                        Score = delta
                    };
                    await _leaderboardRepo.AddOrUpdateAsync(entry, ct);
                }
                else
                {
                    existing.Score += delta;
                    existing.TeamName = request.TeamName;
                    await _leaderboardRepo.AddOrUpdateAsync(existing, ct);
                }

                // Publish leaderboard snapshot for all participants to see
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

            if (!isCorrect)
            {
                _logger.LogInformation("Incorrect answer for QuestionId={QuestionId}: correct={Correct}, selected={Selected}",
                    request.QuestionId, correctIndex, selectedIndex);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to do immediate leaderboard update");
        }

        return new AnswerResult(isCorrect, delta, position);
    }
}
