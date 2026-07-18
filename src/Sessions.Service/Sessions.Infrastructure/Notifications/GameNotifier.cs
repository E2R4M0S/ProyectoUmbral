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

    public async Task NotifyClueReleased(Guid sessionId, Guid? teamId, Guid? userId, object clueData, CancellationToken ct = default)
    {
        try
        {
            await _httpClient.PostAsJsonAsync($"/internal/notifications/clue-released", new
            {
                SessionId = sessionId,
                TeamId = teamId,
                UserId = userId,
                ClueData = clueData
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send clue release notification for {SessionId}", sessionId);
        }
    }

    public async Task NotifyQuestionClosed(Guid sessionId, Guid questionId, Guid correctAnswerId, string? correctAnswerText, CancellationToken ct = default)
    {
        try
        {
            await _httpClient.PostAsJsonAsync($"/internal/notifications/question-closed", new
            {
                SessionId = sessionId,
                QuestionId = questionId,
                CorrectAnswerId = correctAnswerId,
                CorrectAnswerText = correctAnswerText
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send question closed notification for {SessionId}", sessionId);
        }
    }

    public async Task NotifyGateOpenedAsync(Guid sessionId, int nextStageIndex, CancellationToken ct = default)
    {
        try
        {
            await _httpClient.PostAsJsonAsync("/internal/notifications/gate-opened", new
            {
                SessionId = sessionId,
                NextStageIndex = nextStageIndex
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send gate-opened notification for {SessionId}", sessionId);
        }
    }

    public async Task NotifyRankingUpdatedAsync(Guid sessionId, IEnumerable<SessionRankingEntry> entries, CancellationToken ct = default)
    {
        try
        {
            var leaderboardEntries = entries
                .OrderByDescending(e => e.Score)
                .Select(e => new
                {
                    Id = Guid.NewGuid(),
                    QuizId = sessionId,
                    TeamId = e.TeamId ?? e.UserId ?? Guid.Empty,
                    TeamName = e.DisplayName,
                    e.Score,
                    UpdatedAt = DateTime.UtcNow
                })
                .ToList();

            await _httpClient.PostAsJsonAsync("/internal/events/LeaderboardUpdated", leaderboardEntries, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send ranking update for {SessionId}", sessionId);
        }
    }

    public async Task NotifyTeamStageAdvancedAsync(Guid sessionId, Guid teamId, int newStageOrder, int totalStages, CancellationToken ct = default)
    {
        try
        {
            await _httpClient.PostAsJsonAsync("/internal/notifications/team-stage-advanced", new
            {
                SessionId = sessionId,
                TeamId = teamId,
                NewStageOrder = newStageOrder,
                TotalStages = totalStages
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send team stage advance notification for {SessionId}", sessionId);
        }
    }
}
