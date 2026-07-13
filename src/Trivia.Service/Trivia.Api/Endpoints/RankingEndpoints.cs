using MediatR;
using Trivia.Application.Common.Interfaces;
using Trivia.Application.Trivias.Ranking;

namespace Trivia.Api.Endpoints;

public static class RankingEndpoints
{
    public static void MapRankingEndpoints(this WebApplication app)
    {
        app.MapGet("/ranking/{quizId:guid}", async (
            Guid quizId,
            ILeaderboardRepository leaderboard,
            ILogger<Program> logger) =>
        {
            var entries = await leaderboard.GetByQuizAsync(quizId);
            var result = entries
                .Select((e, i) => new { position = i + 1, teamName = e.TeamName, score = e.Score, userId = e.TeamId.ToString() });
            return Results.Ok(result);
        })
        .WithName("GetRankingByQuiz")
        .RequireAuthorization();

        app.MapPost("/ranking/{sessionId:guid}/update", async (
            Guid sessionId,
            IMediator mediator,
            ILogger<Program> logger) =>
        {
            try
            {
                var ranking = await mediator.Send(new UpdateRankingCommand(sessionId));
                logger.LogInformation("Ranking updated for session {SessionId}", sessionId);
                return Results.Ok(new { updated = true, ranking });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to update ranking for session {SessionId}", sessionId);
                return Results.Problem("Failed to update ranking");
            }
        })
        .WithName("UpdateRanking")
        .RequireAuthorization("operator_or_admin");
    }
}
