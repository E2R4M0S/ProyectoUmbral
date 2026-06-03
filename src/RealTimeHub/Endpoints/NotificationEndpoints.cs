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
public record RankingEntryDto(int Position, string TeamName, int Score);
public record RankingUpdatedNotification(Guid SessionId, List<RankingEntryDto> Ranking);
