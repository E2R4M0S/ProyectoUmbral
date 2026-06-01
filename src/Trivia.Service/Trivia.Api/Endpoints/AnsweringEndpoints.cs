using MediatR;
using Microsoft.AspNetCore.Mvc;
using Trivia.Application.Trivias.Clues;
<<<<<<< HEAD
using Trivia.Application.Trivias.Answers;
=======
>>>>>>> 61b3cec (feat(hu-39): countdown timer frontend + backend endpoints and skeleton for answer submission and RabbitMQ consumer)

namespace Trivia.Api.Endpoints;

public static class AnsweringEndpoints
{
    // Placeholder HTTP endpoint for publishing participant responses via SignalR/HTTP bridge
    public static void MapAnsweringEndpoints(this WebApplication app)
    {
        app.MapPost("/api/trivia/answers", async ([FromBody] ParticipantAnswerRequest req, IMediator mediator, ILogger<Program> logger) =>
        {
            try
            {
<<<<<<< HEAD
                // Delegate to SubmitAnswerCommand which persists the answer and publishes an integration event
=======
                // Delegate to SubmitAnswerCommand which publishes an integration event
>>>>>>> 61b3cec (feat(hu-39): countdown timer frontend + backend endpoints and skeleton for answer submission and RabbitMQ consumer)
                await mediator.Send(new Trivia.Application.Trivias.Answers.SubmitAnswerCommand(req.QuizId, req.TeamId, req.QuestionId, req.AnswerId, req.Timestamp));
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
