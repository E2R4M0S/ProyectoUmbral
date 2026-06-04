using MediatR;
using Microsoft.AspNetCore.Mvc;
using Trivia.Application.Trivias.QuizBank;

namespace Trivia.Api.Endpoints;

public static class QuizBankEndpoints
{
    public static void MapQuizBankEndpoints(this WebApplication app)
    {
        app.MapPost("/api/quizzes", async (
            [FromBody] CreateQuizCommand command,
            IMediator mediator,
            ILogger<Program> logger) =>
        {
            try
            {
                var id = await mediator.Send(command);
                logger.LogInformation("Quiz created: {Id}", id);
                return Results.Created($"/api/quizzes/{id}", new { id });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create quiz");
                return Results.Problem("Failed to create quiz");
            }
        })
        .WithName("CreateQuiz")
        .RequireAuthorization("operator_or_admin");

        app.MapGet("/api/quizzes", async (
            IMediator mediator) =>
        {
            var quizzes = await mediator.Send(new ListQuizzesQuery());
            return Results.Ok(quizzes);
        })
        .WithName("ListQuizzes")
        .RequireAuthorization("operator_or_admin");

        app.MapGet("/api/quizzes/{id:guid}", async (
            Guid id,
            IMediator mediator) =>
        {
            var quiz = await mediator.Send(new GetQuizQuery(id));
            if (quiz == null) return Results.NotFound();
            return Results.Ok(quiz);
        })
        .WithName("GetQuiz")
        .RequireAuthorization("operator_or_admin");
    }
}
