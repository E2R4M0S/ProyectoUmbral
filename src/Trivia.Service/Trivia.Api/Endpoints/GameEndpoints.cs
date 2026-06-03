using MediatR;
using Trivia.Application.Trivias.Game;

namespace Trivia.Api.Endpoints;

public static class GameEndpoints
{
    public static void MapGameEndpoints(this WebApplication app)
    {
        app.MapPost("/games/{sessionId:guid}/end", async (
            Guid sessionId,
            IMediator mediator,
            ILogger<Program> logger) =>
        {
            try
            {
                await mediator.Send(new EndTriviaGameCommand(sessionId));
                logger.LogInformation("Trivia game ended for session {SessionId}", sessionId);
                return Results.Ok(new { ended = true, sessionId });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to end trivia game for session {SessionId}", sessionId);
                return Results.Problem("Failed to end trivia game");
            }
        })
        .WithName("EndTriviaGame")
        .RequireAuthorization("operator_or_admin");
    }
}
