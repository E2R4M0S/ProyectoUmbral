using MediatR;
using Microsoft.AspNetCore.Mvc;
using Trivia.Application.Trivias.Progress;
using Trivia.Application.Trivias.Clues;

namespace Trivia.Api.Endpoints;

public static class ProgressEndpoints
{
    public static void MapProgressEndpoints(this WebApplication app)
    {
        app.MapPost("/api/trivia/{quizId:guid}/progress", async ([FromRoute] Guid quizId, [FromBody] object progress, IMediator mediator, ILogger<Program> logger) =>
        {
            try
            {
                await mediator.Send(new UpdateProgressCommand(quizId, progress));
                return Results.Ok();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to update progress for QuizId={QuizId}", quizId);
                return Results.Problem("Failed to update progress", statusCode: 500);
            }
        })
        .WithName("UpdateProgress")
        .RequireAuthorization("operator_or_admin");

        app.MapPost("/api/trivia/{quizId:guid}/clues", async ([FromRoute] Guid quizId, [FromBody] ClueRequest body, IMediator mediator, ILogger<Program> logger) =>
        {
            try
            {
                await mediator.Send(new ReleaseClueCommand(quizId, body.TeamId, body.ClueData));
                return Results.Ok();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to release clue for QuizId={QuizId}", quizId);
                return Results.Problem("Failed to release clue", statusCode: 500);
            }
        })
        .WithName("ReleaseClue")
        .RequireAuthorization("operator_or_admin");
    }

    public record ClueRequest(Guid? TeamId, object ClueData);
}
