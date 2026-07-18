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
            // Targeting (TeamId/UserId) is resolved client-side: every client in the session
            // group receives the notification and decides whether it applies to them. There's
            // no per-team/per-user SignalR group to join, so a server-side-only group send
            // would silently drop targeted clues.
            await hubContext.Clients.Group(notification.SessionId.ToString())
                .SendAsync("ClueReleased", notification, CancellationToken.None);
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
        app.MapPost("/internal/notifications/gate-opened", async (
            [FromBody] GateOpenedNotification notification,
            IHubContext<GameHub> hubContext) =>
        {
            await hubContext.Clients.Group(notification.SessionId.ToString())
                .SendAsync("GateOpened", notification, CancellationToken.None);
            return Results.Ok();
        });

        app.MapPost("/internal/notifications/team-answer-submitted", async (
            [FromBody] TeamAnswerSubmittedNotification notification,
            IHubContext<GameHub> hubContext) =>
        {
            await hubContext.Clients.Group(notification.SessionId.ToString())
                .SendAsync("TeamAnswerSubmitted", notification, CancellationToken.None);
            return Results.Ok();
        });

        app.MapPost("/internal/notifications/team-stage-advanced", async (
            [FromBody] TeamStageAdvancedNotification notification,
            IHubContext<GameHub> hubContext) =>
        {
            await hubContext.Clients.Group(notification.SessionId.ToString())
                .SendAsync("TeamStageAdvanced", notification, CancellationToken.None);
            return Results.Ok();
        });

        app.MapPost("/internal/notifications/question-closed", async (
            [FromBody] QuestionClosedNotification notification,
            IHubContext<GameHub> hubContext) =>
        {
            await hubContext.Clients.Group(notification.SessionId.ToString())
                .SendAsync("QuestionClosed", notification, CancellationToken.None);
            return Results.Ok();
        });

        app.MapPost("/internal/events/QuestionResultsUpdated", async (
            [FromBody] QuestionResultsPayload payload,
            IHubContext<GameHub> hubContext) =>
        {
            await hubContext.Clients.Group(payload.QuizId.ToString())
                .SendAsync("QuestionResultsUpdated", payload, CancellationToken.None);
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
                teamName = e.TeamName ?? e.TeamId.ToString()?.Substring(0, 8) ?? $"Jugador {i + 1}",
                score = e.Score,
                userId = e.TeamId.ToString()
            }).ToList();

            var notification = new { SessionId = sessionId, Ranking = ranking };
            await hubContext.Clients.Group(sessionId.ToString())
                .SendAsync("RankingUpdated", notification, CancellationToken.None);
            return Results.Ok();
        });

        // Authoritative session-wide ranking (treasure + trivia), pushed by Sessions.Service
        // after every score change. Kept on a separate event from the trivia-only
        // RankingUpdated so the trivia leaderboard broadcast can no longer overwrite the
        // participant's accumulated session score on the client.
        app.MapPost("/internal/notifications/session-ranking", async (
            [FromBody] SessionRankingNotification notification,
            IHubContext<GameHub> hubContext) =>
        {
            await hubContext.Clients.Group(notification.SessionId.ToString())
                .SendAsync("SessionRankingUpdated", notification, CancellationToken.None);
            return Results.Ok();
        });
    }
}

public record LeaderboardEntryDto(Guid Id, Guid QuizId, Guid TeamId, string? TeamName, int Score, DateTime UpdatedAt);

public record SessionStatusNotification(Guid SessionId, string Status);
public record ProgressNotification(Guid SessionId, object ProgressData);
public record ClueReleasedNotification(Guid SessionId, Guid? TeamId, Guid? UserId, object ClueData);
public record QuestionAskedNotification(Guid SessionId, Guid QuestionId, string QuestionText, string[] Options, int TimeLimitSeconds, DateTime AskedAt);
public record QuestionResultsNotification(Guid QuizId, Guid? SessionId, Guid QuestionId, object Results);
public record QuestionClosedNotification(Guid SessionId, Guid QuestionId, Guid CorrectAnswerId, string? CorrectAnswerText);
public record RankingEntryDto(int Position, string TeamName, int Score);
public record RankingUpdatedNotification(Guid SessionId, List<RankingEntryDto> Ranking);
public record SessionRankingNotification(Guid SessionId, List<object> Ranking);
public record GateOpenedNotification(Guid SessionId, int NextStageIndex);
public record TeamStageAdvancedNotification(Guid SessionId, Guid TeamId, int NewStageOrder, int TotalStages);
public record TeamAnswerSubmittedNotification(Guid SessionId, Guid TeamId, Guid QuestionId, int SelectedIndex, bool IsCorrect, int PointsAwarded);
public record QuestionResultsPayload(Guid QuizId, Guid QuestionId, List<AnswerResultItemDto> Results);
public record AnswerResultItemDto(Guid AnswerId, string Text, int Count, double Percentage);
