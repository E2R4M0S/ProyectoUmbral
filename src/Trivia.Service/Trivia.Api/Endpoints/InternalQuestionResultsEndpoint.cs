using MediatR;
using Microsoft.AspNetCore.Mvc;
using Trivia.Application.Trivias.Questions;

namespace Trivia.Api.Endpoints;

public static class InternalQuestionResultsEndpoint
{
    public static void MapInternalQuestionResults(this WebApplication app)
    {
        app.MapPost("/internal/trivia/{quizId:guid}/questions/{questionId:guid}/results", async ([FromRoute] Guid quizId, [FromRoute] Guid questionId, IMediator mediator, ILogger<Program> logger) =>
        {
            try
            {
                await mediator.Send(new QuestionResultsCommand(quizId, questionId));
                return Results.Ok();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to compute question results for QuizId={QuizId} QuestionId={QuestionId}", quizId, questionId);
                return Results.Problem("Failed to compute question results", statusCode: 500);
            }
        }).WithTags("internal");
    }
}
