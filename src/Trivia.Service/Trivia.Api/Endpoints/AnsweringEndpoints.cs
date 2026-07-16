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
        app.MapPost("/answers", async ([FromBody] ParticipantAnswerRequest req, IMediator mediator, ILogger<Program> logger) =>
        {
            try
            {
                // Delegate to SubmitAnswerCommand which persists the answer and publishes an integration event
                var result = await mediator.Send(new SubmitAnswerCommand(req.QuizId, req.TeamId, req.TeamName, req.QuestionId, req.AnswerId, req.Timestamp, req.AskedAt, req.TimeLimitSeconds, req.UserId));
                return Results.Ok(new { received = true, isCorrect = result.IsCorrect, pointsAwarded = result.PointsAwarded, position = result.Position });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to register participant answer");
                return Results.Problem("Failed to register participant answer", statusCode: 500);
            }
        }).RequireAuthorization();

        // Internal test endpoint: useful for local CI/manual tests when authentication is not available.
        // Enabled only when environment variable ENABLE_INTERNAL_TESTS is set to "true".
        try
        {
            var enable = string.Equals(Environment.GetEnvironmentVariable("ENABLE_INTERNAL_TESTS"), "true", StringComparison.OrdinalIgnoreCase);
            if (enable)
            {
                app.MapPost("/internal/test/trivia/answers", async ([FromBody] ParticipantAnswerRequest req, IMediator mediator, ILogger<Program> logger) =>
                {
                    try
                    {
                        var result = await mediator.Send(new SubmitAnswerCommand(req.QuizId, req.TeamId, req.TeamName, req.QuestionId, req.AnswerId, req.Timestamp, req.AskedAt, req.TimeLimitSeconds, req.UserId));
                        return Results.Ok(new { received = true, test = true, isCorrect = result.IsCorrect, pointsAwarded = result.PointsAwarded });
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to register participant answer (internal test)");
                        return Results.Problem("Failed to register participant answer", statusCode: 500);
                    }
                });
            }
        }
        catch { }
    }

    public record ParticipantAnswerRequest(Guid QuizId, Guid TeamId, string TeamName, Guid QuestionId, Guid AnswerId, DateTime Timestamp, DateTime AskedAt, int TimeLimitSeconds, Guid UserId = default);
}
