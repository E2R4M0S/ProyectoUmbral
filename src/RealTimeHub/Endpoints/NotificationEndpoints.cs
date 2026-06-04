using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc;
using RealTimeHub.Hubs;

namespace RealTimeHub.Endpoints;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this WebApplication app)
    {
        app.MapPost("/internal/notifications/session-status", async (
            [FromBody] SessionStatusNotification notification,
            IHubContext<GameHub> hubContext) =>
        {
            await hubContext.Clients.Group(notification.SessionId.ToString())
                .SendAsync("SessionStatusChanged", notification, CancellationToken.None);
            return Results.Ok();
        });

        app.MapPost("/internal/notifications/progress", async (
            [FromBody] ProgressNotification notification,
            IHubContext<GameHub> hubContext) =>
        {
            await hubContext.Clients.Group(notification.SessionId.ToString())
                .SendAsync("ProgressUpdated", notification, CancellationToken.None);
            return Results.Ok();
        });

        app.MapPost("/internal/notifications/clue-released", async (
            [FromBody] ClueReleasedNotification notification,
            IHubContext<GameHub> hubContext) =>
        {
            if (notification.TeamId.HasValue)
            {
                await hubContext.Clients.Group(notification.TeamId.Value.ToString())
                    .SendAsync("ClueReleased", notification, CancellationToken.None);
            }
            else
            {
                await hubContext.Clients.Group(notification.SessionId.ToString())
                    .SendAsync("ClueReleased", notification, CancellationToken.None);
            }
            return Results.Ok();
        });

        // Podium notification moved to a dedicated endpoint to avoid duplicate route definitions
        app.MapPost("/internal/notifications/question-asked", async (
            [FromBody] QuestionAskedNotification notification,
            IHubContext<GameHub> hubContext) =>
        {
            await hubContext.Clients.Group(notification.SessionId.ToString())
                .SendAsync("QuestionAsked", notification, CancellationToken.None);
            return Results.Ok();
        });
        app.MapPost("/internal/events/LeaderboardUpdated", async (
            [FromBody] List<LeaderboardEntryDto> entries,
            IHubContext<GameHub> hubContext) =>
        {
            if (entries == null || entries.Count == 0) return Results.Ok();
            var sessionId = entries[0].QuizId;
            var ranking = entries.Select((e, i) => new
            {
                position = i + 1,
                teamName = e.TeamId.ToString()?.Substring(0, 8) ?? $"Team {i + 1}",
                score = e.Score
            }).ToList();

            var notification = new { SessionId = sessionId, Ranking = ranking };
            await hubContext.Clients.Group(sessionId.ToString())
                .SendAsync("RankingUpdated", notification, CancellationToken.None);
            return Results.Ok();
        });
    }
}

public record LeaderboardEntryDto(Guid Id, Guid QuizId, Guid TeamId, int Score, DateTime UpdatedAt);

public record SessionStatusNotification(Guid SessionId, string Status);
public record ProgressNotification(Guid SessionId, object ProgressData);
public record ClueReleasedNotification(Guid SessionId, Guid? TeamId, object ClueData);
public record QuestionAskedNotification(Guid SessionId, Guid QuestionId, string QuestionText, string[] Options, int TimeLimitSeconds);
public record QuestionResultsNotification(Guid QuizId, Guid? SessionId, Guid QuestionId, object Results);
public record QuestionClosedNotification(Guid SessionId, Guid QuestionId, Guid CorrectAnswerId, string? CorrectAnswerText);
public record RankingEntryDto(int Position, string TeamName, int Score);
public record RankingUpdatedNotification(Guid SessionId, List<RankingEntryDto> Ranking);
