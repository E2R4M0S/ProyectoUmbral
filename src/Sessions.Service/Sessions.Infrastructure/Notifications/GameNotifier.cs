using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Sessions.Application.Common.Interfaces;

namespace Sessions.Infrastructure.Notifications;

public class GameNotifier : IGameNotifier
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GameNotifier> _logger;

    public GameNotifier(HttpClient httpClient, ILogger<GameNotifier> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task NotifySessionStatusChanged(Guid sessionId, string status, CancellationToken ct = default)
    {
        try
        {
            await _httpClient.PostAsJsonAsync($"/internal/notifications/session-status", new
            {
                SessionId = sessionId,
                Status = status
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send session status notification for {SessionId}", sessionId);
        }
    }

    public async Task NotifyProgressUpdated(Guid sessionId, object progressData, CancellationToken ct = default)
    {
        try
        {
            await _httpClient.PostAsJsonAsync($"/internal/notifications/progress", new
            {
                SessionId = sessionId,
                ProgressData = progressData
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send progress notification for {SessionId}", sessionId);
        }
    }

        public async Task NotifyClueReleased(Guid sessionId, Guid? teamId, object clueData, CancellationToken ct = default)
        {
            try
            {
                await _httpClient.PostAsJsonAsync($"/internal/notifications/clue-released", new
                {
                    SessionId = sessionId,
                    TeamId = teamId,
                    ClueData = clueData
                }, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send clue release notification for {SessionId}", sessionId);
            }
        }

        public async Task NotifyQuestionResults(Guid quizId, Guid questionId, object results, CancellationToken ct = default)
        {
            try
            {
                await _httpClient.PostAsJsonAsync($"/internal/notifications/question-results", new
                {
                    QuizId = quizId,
                    QuestionId = questionId,
                    Results = results
                }, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send question results notification for QuizId={QuizId} QuestionId={QuestionId}", quizId, questionId);
            }
        }
}
