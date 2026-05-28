using MediatR;
using Sessions.Application.Sessions.Create;
using Microsoft.AspNetCore.Mvc;

namespace Sessions.Api.Endpoints;

public static class CreateSessionEndpoint
{
    public static void MapCreateSessionEndpoint(this WebApplication app)
    {
        app.MapPost("/api/sessions", async (
            [FromBody] CreateSessionCommand command,
            IMediator mediator,
            ILogger<Program> logger) =>
        {
            try
            {
                var result = await mediator.Send(command);

                logger.LogInformation(
                    "Session created successfully: Id={SessionId}, Name={Name}, Pin={Pin}",
                    result.Id, result.Name, result.Pin);

                return Results.Created(
                    $"/api/sessions/{result.Id}",
                    new
                    {
                        id = result.Id,
                        name = result.Name,
                        missionId = result.MissionId,
                        pin = result.Pin,
                        status = result.Status,
                        startedAt = result.StartedAt,
                        endedAt = result.EndedAt,
                        createdAt = result.CreatedAt
                    });
            }
            catch (FluentValidation.ValidationException ex)
            {
                logger.LogWarning(
                    "Session creation validation failed: {Message}", ex.Message);

                return Results.BadRequest(new
                {
                    error = "Validation failed",
                    details = ex.Errors.Select(e => new
                    {
                        field = e.PropertyName,
                        message = e.ErrorMessage
                    })
                });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("unique PIN"))
            {
                logger.LogWarning(
                    "Session creation conflict: PIN generation failed — {Message}", ex.Message);

                return Results.Conflict(new
                {
                    error = "Conflict",
                    details = new[]
                    {
                        new { field = "Pin", message = "Could not generate a unique PIN. Please try again." }
                    }
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Session creation failed due to an unexpected error");

                var problem = new
                {
                    statusCode = StatusCodes.Status500InternalServerError,
                    title = "Session creation failed",
                    detail = "An unexpected error occurred while creating the session. Please try again later."
                };

                return Results.Problem(
                    problem.title,
                    null,
                    problem.statusCode,
                    problem.title,
                    problem.detail);
            }
        })
        .WithName("CreateSession")
        .RequireAuthorization("operator_or_admin");
    }
}
