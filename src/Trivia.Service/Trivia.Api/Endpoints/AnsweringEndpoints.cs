using MediatR;
using Microsoft.AspNetCore.Mvc;
using Trivia.Application.Trivias.Clues;
using Trivia.Application.Trivias.Answers;

namespace Trivia.Api.Endpoints;

public static class AnsweringEndpoints
{
    // HTTP endpoint for participant answers
    public static void MapAnsweringEndpoints(this WebApplication app)
    {
        app.MapPost("/api/trivia/answers", async ([FromBody] ParticipantAnswerRequest req, IMediator mediator, ILogger<Program> logger) =>
        {
            try
            {
                // Delegate to SubmitAnswerCommand which persists the answer and publishes an integration event
                await mediator.Send(new SubmitAnswerCommand(req.QuizId, req.TeamId, req.QuestionId, req.AnswerId, req.Timestamp));
                return Results.Ok(new { received = true });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to register participant answer");
                return Results.Problem("Failed to register participant answer", statusCode: 500);
            }
        }).RequireAuthorization();
    }

    public record ParticipantAnswerRequest(Guid QuizId, Guid TeamId, Guid QuestionId, Guid AnswerId, DateTime Timestamp);
}
