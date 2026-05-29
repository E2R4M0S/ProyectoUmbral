using MediatR;
using Sessions.Application.Sessions.Transition;
using Microsoft.AspNetCore.Mvc;

namespace Sessions.Api.Endpoints;

public static class FinishSessionEndpoint
{
    public static void MapFinishSessionEndpoint(this WebApplication app)
    {
        app.MapPost("/api/sessions/{id:guid}/finish", async (
            [FromRoute] Guid id,
            IMediator mediator,
            ILogger<Program> logger) =>
        {
            try
            {
                var command = new TransitionSessionCommand(id, "Finished");
                await mediator.Send(command);

                logger.LogInformation(
                    "Session finished successfully: Id={SessionId}",
                    id);

                return Results.Ok(new { id, status = "Finished" });
            }
            catch (FluentValidation.ValidationException ex)
            {
                logger.LogWarning(
                    "Finish session validation failed: {Message}", ex.Message);

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
                    "Session finish failed: {Message}", ex.Message);

                return Results.BadRequest(new
                {
                    error = "Cannot finish session",
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Session finish failed due to an unexpected error");

                return Results.Problem(
                    "Session finish failed",
                    null,
                    StatusCodes.Status500InternalServerError,
                    "Session finish failed",
                    "An unexpected error occurred while finishing the session.");
            }
        })
        .WithName("FinishSession")
        .RequireAuthorization("operator_or_admin");
    }
}
