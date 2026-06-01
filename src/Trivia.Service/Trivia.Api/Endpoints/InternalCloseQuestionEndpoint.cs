using Microsoft.AspNetCore.Mvc;
using Trivia.Application.Trivias.Leaderboard;

namespace Trivia.Api.Endpoints;

public static class InternalCloseQuestionEndpoint
{
    public static void MapInternalCloseQuestion(this WebApplication app)
    {
        app.MapPost("/internal/trivia/{quizId:guid}/close-question", async ([FromRoute] Guid quizId, IMediator mediator, ILogger<Program> logger) =>
        {
            try
            {
                await mediator.Send(new CloseQuestionCommand(quizId));
                return Results.Ok();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to close question for quiz {QuizId}", quizId);
                return Results.Problem("Failed to close question", statusCode: 500);
            }
        }).WithTags("internal");
    }
}
