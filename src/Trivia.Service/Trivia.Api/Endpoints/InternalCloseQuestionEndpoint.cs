using MediatR;
using Microsoft.AspNetCore.Mvc;
using Trivia.Application.Trivias.Leaderboard;

namespace Trivia.Api.Endpoints;

public static class InternalCloseQuestionEndpoint
{
    public static void MapInternalCloseQuestion(this WebApplication app)
    {
        app.MapPost("/internal/trivia/{sessionId:guid}/questions/{questionId:guid}/close", async ([FromRoute] Guid sessionId, [FromRoute] Guid questionId, IMediator mediator, ILogger<Program> logger) =>
        {
            try
            {
                await mediator.Send(new CloseQuestionCommand(sessionId, questionId));
                return Results.Ok();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to close question {QuestionId} for session {SessionId}", questionId, sessionId);
                return Results.Problem("Failed to close question", statusCode: 500);
            }
        }).WithTags("internal");
    }
}
