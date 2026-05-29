using MediatR;
using Sessions.Application.Sessions.Transition;
using Microsoft.AspNetCore.Mvc;

namespace Sessions.Api.Endpoints;

public static class TransitionSessionEndpoint
{
    public static void MapTransitionSessionEndpoint(this WebApplication app)
    {
        app.MapPatch("/api/sessions/{id:guid}/status", async (
            [FromRoute] Guid id,
            [FromBody] TransitionSessionCommand command,
            IMediator mediator,
            ILogger<Program> logger) =>
        {
            try
            {
                var updatedCommand = command with { Id = id };
                await mediator.Send(updatedCommand);

                logger.LogInformation(
                    "Session status transitioned successfully: Id={SessionId}",
                    id);

                return Results.NoContent();
            }
            catch (FluentValidation.ValidationException ex)
            {
                logger.LogWarning(
                    "Session transition validation failed: {Message}", ex.Message);

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
            catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
            {
                logger.LogWarning(
                    "Session not found: Id={SessionId}", id);

                return Results.NotFound(new
                {
                    error = "Not Found",
                    message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(
                    "Invalid session status transition: {Message}", ex.Message);

                return Results.BadRequest(new
                {
                    error = "Invalid status transition",
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Session status transition failed due to an unexpected error");

                var problem = new
                {
                    statusCode = StatusCodes.Status500InternalServerError,
                    title = "Session status transition failed",
                    detail = "An unexpected error occurred while transitioning session status. Please try again later."
                };

                return Results.Problem(
                    problem.title,
                    null,
                    problem.statusCode,
                    problem.title,
                    problem.detail);
            }
        })
        .WithName("TransitionSession")
        .RequireAuthorization("operator_or_admin");
    }
}