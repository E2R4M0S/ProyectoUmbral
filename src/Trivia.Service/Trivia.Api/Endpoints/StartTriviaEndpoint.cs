using MediatR;
using Microsoft.AspNetCore.Mvc;
using Trivia.Application.Trivias.StartTrivia;

namespace Trivia.Api.Endpoints;

public static class StartTriviaEndpoint
{
    public static void MapStartTrivia(this WebApplication app)
    {
        app.MapPost("/api/trivia/{quizId:guid}/start", async ([FromRoute] Guid quizId, IMediator mediator, ILogger<Program> logger) =>
        {
            try
            {
                await mediator.Send(new StartTriviaCommand(quizId));
                return Results.Ok(new { id = quizId, status = "Started" });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
            {
                logger.LogWarning(ex, "Quiz not found: {QuizId}", quizId);
                return Results.NotFound(new { error = "Not Found", message = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to start trivia for QuizId={QuizId}", quizId);
                return Results.Problem("Failed to start trivia", statusCode: 500);
            }
        })
        .WithName("StartTrivia")
        .RequireAuthorization("operator_or_admin");
    }
}
