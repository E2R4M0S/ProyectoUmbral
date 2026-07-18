using System.Net.Http.Json;
using System.Text.Json;
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
    private readonly IScoringStrategy _scoringStrategy;

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

    // RB-03: no answers may be accepted once the (Sessions.Service) session backing this game
    // is Paused, Finished or Cancelled. Best-effort — quizId doubles as the sessionId (see
    // QuestionCard.tsx / TriviaAnswerSubmittedConsumer) — if Sessions.Service can't be reached
    // we fail open rather than blocking live gameplay on a transient network issue.
    private static readonly HashSet<string> BlockedSessionStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Paused", "Finished", "Cancelled"
    };

    private async Task<string?> GetBlockedSessionStatusAsync(Guid quizId, CancellationToken ct)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("sessionsService");
            var response = await client.GetAsync($"/{quizId}", ct);
            if (!response.IsSuccessStatusCode) return null;

            var session = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            if (!session.TryGetProperty("status", out var statusProp)) return null;

            var status = statusProp.GetString();
            return status is not null && BlockedSessionStatuses.Contains(status) ? status : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to verify session status for quiz {QuizId}; allowing the answer", quizId);
            return null;
        }
    }

    public async Task<AnswerResult> Handle(SubmitAnswerCommand request, CancellationToken ct)
    {
        var blockedStatus = await GetBlockedSessionStatusAsync(request.QuizId, ct);
        if (blockedStatus is not null)
        {
            _logger.LogInformation(
                "Rejecting answer for QuizId={QuizId}: session status is {Status}", request.QuizId, blockedStatus);
            return new AnswerResult(false, 0, 0, Rejected: true, RejectReason: $"Session is {blockedStatus}");
        }

        var correctIndex = AskQuestionCommandHandler.CorrectAnswers.GetValueOrDefault(request.QuestionId, -1);
        var selectedIndex = int.TryParse(request.AnswerId.ToString()?.Last().ToString(), out var idx) ? idx : -1;
        bool isCorrect = correctIndex >= 0 && selectedIndex == correctIndex;

        // Per-user idempotency: each participant submits exactly once per question.
        if (request.UserId != Guid.Empty)
        {
            var userKey = (request.QuestionId, request.UserId);
            if (!AskQuestionCommandHandler.UserAnswers.TryAdd(userKey, selectedIndex))
            {
                var existingIdx = AskQuestionCommandHandler.UserAnswers[userKey];
                var wasCorrect = correctIndex >= 0 && existingIdx == correctIndex;
                _logger.LogInformation("User {UserId} already answered question {QuestionId} — ignoring duplicate", request.UserId, request.QuestionId);
                return new AnswerResult(wasCorrect, 0, 0);
            }
        }

        // Per-team idempotency: only the first submission per team triggers scoring and SignalR notification.
        var teamKey = (request.QuestionId, request.TeamId);
        bool firstForTeam = AskQuestionCommandHandler.TeamAnswers.TryAdd(teamKey, selectedIndex);

        if (!firstForTeam && request.UserId == Guid.Empty)
        {
            // Legacy path (no userId): team already answered, reject entirely.
            var existingIndex = AskQuestionCommandHandler.TeamAnswers[teamKey];
            var wasCorrect = correctIndex >= 0 && existingIndex == correctIndex;
            _logger.LogInformation("Team {TeamId} already answered question {QuestionId} — ignoring duplicate", request.TeamId, request.QuestionId);
            return new AnswerResult(wasCorrect, 0, 0);
        }

        // Save the participant's answer to DB so answerCount increments for every member.
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
        if (_answerRepo is not null)
            await _answerRepo.AddAsync(answer, ct);

        // Sync submission: not the first for the team — skip scoring and notification.
        if (!firstForTeam)
        {
            var existingPoints = AskQuestionCommandHandler.TeamAnswerPoints.GetValueOrDefault(teamKey, 0);
            return new AnswerResult(isCorrect, existingPoints, 0);
        }

        // First submission for this team: calculate score, notify team members, update leaderboard.
        int earlyDelta = 0;
        if (isCorrect && request.AskedAt != default)
        {
            var elapsed = request.Timestamp - request.AskedAt;
            earlyDelta = _scoringStrategy.CalculateScore(elapsed, request.TimeLimitSeconds);
        }
        AskQuestionCommandHandler.TeamAnswerPoints[teamKey] = earlyDelta;

        // Notify all team members immediately via SignalR.
        try
        {
            var rtClient = _httpClientFactory.CreateClient("realTimeHub");
            await rtClient.PostAsJsonAsync("/internal/notifications/team-answer-submitted", new
            {
                SessionId = request.QuizId,
                TeamId = request.TeamId,
                QuestionId = request.QuestionId,
                SelectedIndex = selectedIndex,
                IsCorrect = isCorrect,
                PointsAwarded = earlyDelta,
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to notify team answer submitted for team {TeamId}", request.TeamId);
        }

        // Publish integration event to RabbitMQ.
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

        // Update leaderboard.
        int position = 0;
        int delta = 0;

        try
        {
            if (_leaderboardRepo is not null)
            {
                if (isCorrect)
                {
                    var timestamps = AskQuestionCommandHandler.CorrectAnswerTimestamps
                        .GetOrAdd(request.QuestionId, _ => new List<DateTime>());

                    lock (timestamps)
                    {
                        timestamps.Add(request.Timestamp);
                        timestamps.Sort();
                        position = timestamps.IndexOf(request.Timestamp) + 1;
                    }

                    var timeElapsed = request.Timestamp - request.AskedAt;
                    delta = _scoringStrategy.CalculateScore(timeElapsed, request.TimeLimitSeconds);
                }

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

                if (isCorrect && delta > 0)
                {
                    try
                    {
                        var sessionsClient = _httpClientFactory.CreateClient("sessionsService");
                        await sessionsClient.PostAsJsonAsync("/internal/teams/score",
                            new { TeamId = request.TeamId, Delta = delta }, ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to notify Sessions.Service of team score for team {TeamId}", request.TeamId);
                    }

                    // Solo participants also need their score recorded (QuizId == SessionId convention)
                    if (request.UserId != Guid.Empty)
                    {
                        try
                        {
                            var sessionsClient = _httpClientFactory.CreateClient("sessionsService");
                            await sessionsClient.PostAsJsonAsync("/internal/participants/score",
                                new { SessionId = request.QuizId, UserId = request.UserId, Delta = delta }, ct);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to notify Sessions.Service of participant score for user {UserId}", request.UserId);
                        }
                    }
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
