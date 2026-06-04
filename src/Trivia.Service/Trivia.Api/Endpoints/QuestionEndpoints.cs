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
                var questionId = await mediator.Send(command);
                logger.LogInformation("Question asked for session {SessionId}", command.SessionId);
                return Results.Ok(new { asked = true, sessionId = command.SessionId, questionId });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to ask question");
                return Results.Problem("Failed to ask question");
            }
        })
        .WithName("AskQuestion")
        .RequireAuthorization("operator_or_admin");

        app.MapGet("/questions/{questionId:guid}/answer-count", async (
            Guid questionId,
            IMediator mediator) =>
        {
            var count = await mediator.Send(new GetAnswerCountQuery(questionId));
            return Results.Ok(new { answerCount = count });
        })
        .WithName("GetAnswerCount")
        .RequireAuthorization("operator_or_admin");
    }
}
