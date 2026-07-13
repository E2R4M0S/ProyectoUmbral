using Sessions.Application.Common.Interfaces;

namespace Sessions.Api.Endpoints;

public static class ParticipantScoreEndpoint
{
    public static void MapParticipantScoreEndpoints(this WebApplication app)
    {
        // Internal endpoint — called by other services on the Docker network (no auth required)
        app.MapPost("/internal/participants/score", async (
            ParticipantScoreRequest req,
            ISessionRepository repository,
            ILogger<Program> logger) =>
        {
            if (req.Delta <= 0) return Results.Ok();
            await repository.AddParticipantScoreAsync(req.SessionId, req.UserId, req.Delta);
            logger.LogInformation("Added {Delta} pts to participant {UserId} in session {SessionId}", req.Delta, req.UserId, req.SessionId);
            return Results.Ok();
        })
        .WithName("UpdateParticipantScore")
        .AllowAnonymous();

        // Public ranking endpoint — accessible via /api/sessions/participants/ranking
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
    }
}

public record ParticipantScoreRequest(Guid SessionId, Guid UserId, int Delta);
