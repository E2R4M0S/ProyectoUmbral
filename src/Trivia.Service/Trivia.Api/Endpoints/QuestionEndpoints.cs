using MediatR;
using Microsoft.AspNetCore.Mvc;
using Trivia.Application.Trivias.Leaderboard;
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

        // HU-43 + HU-44: close a question — reveals correct answer to participants and sends breakdown to operator
        app.MapPost("/questions/{questionId:guid}/close", async (
            Guid questionId,
            [FromBody] CloseQuestionRequest body,
            IMediator mediator,
            ILogger<Program> logger) =>
        {
            try
            {
                await mediator.Send(new CloseQuestionCommand(body.SessionId, questionId));
                return Results.Ok();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to close question {QuestionId}", questionId);
                return Results.Problem("Failed to close question");
            }
        })
        .WithName("CloseQuestion")
        .RequireAuthorization("operator_or_admin");
    }
}

public record CloseQuestionRequest(Guid SessionId);
