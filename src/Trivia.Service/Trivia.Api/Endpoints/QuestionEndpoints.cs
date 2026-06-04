using MediatR;
using Microsoft.AspNetCore.Mvc;
using Trivia.Application.Trivias.Questions;

namespace Trivia.Api.Endpoints;

public static class QuestionEndpoints
{
    public static void MapQuestionEndpoints(this WebApplication app)
    {
        app.MapPost("/questions/ask", async (
            [FromBody] AskQuestionCommand command,
            IMediator mediator,
            ILogger<Program> logger) =>
        {
            try
            {
                await mediator.Send(command);
                logger.LogInformation("Question asked for session {SessionId}", command.SessionId);
                return Results.Ok(new { asked = true, sessionId = command.SessionId });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to ask question");
                return Results.Problem("Failed to ask question");
            }
        })
        .WithName("AskQuestion")
        .RequireAuthorization("operator_or_admin");
    }
}
