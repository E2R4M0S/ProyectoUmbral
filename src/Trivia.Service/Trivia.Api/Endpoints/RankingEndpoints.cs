using MediatR;
using Trivia.Application.Trivias.Ranking;

namespace Trivia.Api.Endpoints;

public static class RankingEndpoints
{
    public static void MapRankingEndpoints(this WebApplication app)
    {
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
