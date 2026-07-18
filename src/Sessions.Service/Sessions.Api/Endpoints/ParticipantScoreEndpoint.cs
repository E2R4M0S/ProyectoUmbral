using Sessions.Application.Common.Interfaces;
using Sessions.Domain.Entities;
using Sessions.Domain.Enums;

namespace Sessions.Api.Endpoints;

public static class ParticipantScoreEndpoint
{
    public static void MapParticipantScoreEndpoints(this WebApplication app)
    {
        // Internal endpoint — called by Trivia.Service (on the Docker network, no auth required)
        // whenever a participant's trivia answer is scored.
        app.MapPost("/internal/participants/score", async (
            ParticipantScoreRequest req,
            ISessionRepository repository,
            IGameNotifier notifier,
            ILogger<Program> logger) =>
        {
            if (req.Delta <= 0) return Results.Ok();

            // RB-03 / HU-34: once a session is terminal, its score must stay frozen.
            var session = await repository.GetByIdAsync(req.SessionId, CancellationToken.None);
            if (session is null || session.Status is SessionStatus.Finished or SessionStatus.Cancelled)
            {
                logger.LogInformation("Skipped {Delta} pts for participant {UserId}: session {SessionId} is missing or terminal", req.Delta, req.UserId, req.SessionId);
                return Results.Ok();
            }

            await repository.AddParticipantScoreAsync(req.SessionId, req.UserId, req.Delta);

            // RB-07: trivia scoring must be traceable to its origin, same as Treasure evidence.
            await repository.AddAuditEventAsync(SessionAuditEvent.Create(
                req.SessionId,
                SessionAuditEventTypes.TriviaAnswerScored,
                "Puntaje otorgado por respuesta de trivia",
                userId: req.UserId,
                scoreDelta: req.Delta));

            // Bug fix: Trivia.Service broadcasts its own trivia-only leaderboard via
            // RankingUpdated, which used to overwrite the participant's accumulated score.
            // The session-wide ranking (treasure + trivia) is the authoritative source for
            // the score badge, so we emit it on a dedicated event right after the DB write.
            var rankingEntries = await repository.GetSessionRankingAsync(req.SessionId, CancellationToken.None);
            await notifier.NotifySessionRankingUpdatedAsync(req.SessionId, rankingEntries, CancellationToken.None);

            logger.LogInformation("Added {Delta} pts to participant {UserId} in session {SessionId}", req.Delta, req.UserId, req.SessionId);
            return Results.Ok();
        })
        .WithName("UpdateParticipantScore")
        .AllowAnonymous();

        // Internal endpoint — called by Trivia.Service to award points to an entire team
        // when one of its members answers a trivia question correctly.
        app.MapPost("/internal/teams/score", async (
            TeamScoreRequest req,
            ISessionRepository repository,
            IGameNotifier notifier,
            ILogger<Program> logger) =>
        {
            if (req.Delta <= 0) return Results.Ok();

            var team = await repository.GetTeamByIdAsync(req.TeamId, CancellationToken.None);
            if (team is null) return Results.Ok();

            // RB-03 / HU-34: once a session is terminal, its score must stay frozen.
            var session = await repository.GetByIdAsync(team.SessionId, CancellationToken.None);
            if (session is null || session.Status is SessionStatus.Finished or SessionStatus.Cancelled)
            {
                logger.LogInformation("Skipped {Delta} pts for team {TeamId}: session {SessionId} is missing or terminal", req.Delta, req.TeamId, team.SessionId);
                return Results.Ok();
            }

            await repository.AddTeamScoreAsync(req.TeamId, req.Delta);

            // RB-07: same traceability as the solo-participant path above.
            await repository.AddAuditEventAsync(SessionAuditEvent.Create(
                team.SessionId,
                SessionAuditEventTypes.TriviaAnswerScored,
                "Puntaje otorgado al equipo por respuesta de trivia",
                teamId: req.TeamId,
                scoreDelta: req.Delta));

            // See /internal/participants/score: broadcast the full session ranking so the
            // frontend's score badge and leaderboard both reflect the treasure+trivia total.
            var rankingEntries = await repository.GetSessionRankingAsync(team.SessionId, CancellationToken.None);
            await notifier.NotifySessionRankingUpdatedAsync(team.SessionId, rankingEntries, CancellationToken.None);

            logger.LogInformation("Added {Delta} pts to team {TeamId}", req.Delta, req.TeamId);
            return Results.Ok();
        })
        .WithName("UpdateTeamScore")
        .AllowAnonymous();

        // Global ranking — individual users only (HU-47)
        app.MapGet("/participants/ranking", async (
            HttpContext http,
            ISessionRepository repository) =>
        {
            var period = http.Request.Query["period"].ToString();
            DateTime? since = period == "monthly" ? DateTime.UtcNow.AddDays(-30) : null;

            var entries = await repository.GetGlobalParticipantRankingAsync(since);
            var result = entries.Select((e, i) => new
            {
                position = i + 1,
                userId = e.UserId.ToString(),
                displayName = e.Alias,
                totalScore = e.TotalScore,
            });
            return Results.Ok(result);
        })
        .WithName("GetParticipantRanking")
        .RequireAuthorization();

        // Per-session ranking — mixed: teams + solo participants
        app.MapGet("/{sessionId:guid}/ranking", async (
            Guid sessionId,
            ISessionRepository repository) =>
        {
            var entries = await repository.GetSessionRankingAsync(sessionId);
            var result = entries.Select((e, i) => new
            {
                position = i + 1,
                type = e.Type,
                displayName = e.DisplayName,
                score = e.Score,
                memberCount = e.MemberCount,
                teamId = e.TeamId?.ToString(),
                userId = e.UserId?.ToString(),
            });
            return Results.Ok(result);
        })
        .WithName("GetSessionRanking")
        .RequireAuthorization();
    }
}

public record ParticipantScoreRequest(Guid SessionId, Guid UserId, int Delta);
public record TeamScoreRequest(Guid TeamId, int Delta);
