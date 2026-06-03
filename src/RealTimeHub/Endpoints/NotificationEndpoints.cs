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

        // Endpoint to broadcast question results (used by backend services)
        app.MapPost("/internal/notifications/question-results", async (
            [FromBody] QuestionResultsNotification notification,
            IHubContext<GameHub> hubContext) =>
        {
            if (notification.QuizId != Guid.Empty)
            {
                await hubContext.Clients.Group(notification.QuizId.ToString())
                    .SendAsync("QuestionResultsUpdated", notification.Results, CancellationToken.None);
            }
            if (notification.SessionId != null)
            {
                await hubContext.Clients.Group(notification.SessionId.Value.ToString())
                    .SendAsync("QuestionResultsUpdated", notification.Results, CancellationToken.None);
            }
            return Results.Ok();
        });

        // Endpoint to notify participants that a question was closed and include correct answer info
        app.MapPost("/internal/notifications/question-closed", async (
            [FromBody] QuestionClosedNotification notification,
            IHubContext<GameHub> hubContext) =>
        {
            if (notification.SessionId != Guid.Empty)
            {
                await hubContext.Clients.Group(notification.SessionId.ToString())
                    .SendAsync("QuestionClosed", new { notification.QuestionId, notification.CorrectAnswerId, notification.CorrectAnswerText }, CancellationToken.None);
            }
            return Results.Ok();
        });

        // Endpoint to broadcast real-time ranking updates
        app.MapPost("/internal/notifications/ranking-updated", async (
            [FromBody] RankingUpdatedNotification notification,
            IHubContext<GameHub> hubContext) =>
        {
            await hubContext.Clients.Group(notification.SessionId.ToString())
                .SendAsync("RankingUpdated", notification, CancellationToken.None);
            return Results.Ok();
        });
    }
}

public record SessionStatusNotification(Guid SessionId, string Status);
public record ProgressNotification(Guid SessionId, object ProgressData);
public record ClueReleasedNotification(Guid SessionId, Guid? TeamId, object ClueData);
public record QuestionResultsNotification(Guid QuizId, Guid? SessionId, Guid QuestionId, object Results);
public record QuestionClosedNotification(Guid SessionId, Guid QuestionId, Guid CorrectAnswerId, string? CorrectAnswerText);
public record RankingEntryDto(int Position, string TeamName, int Score);
public record RankingUpdatedNotification(Guid SessionId, List<RankingEntryDto> Ranking);
