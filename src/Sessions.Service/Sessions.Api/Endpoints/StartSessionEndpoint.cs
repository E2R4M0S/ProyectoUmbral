using MediatR;
using Sessions.Application.Common.Interfaces;
using Sessions.Application.Sessions.Transition;
using Microsoft.AspNetCore.Mvc;

namespace Sessions.Api.Endpoints;

public static class StartSessionEndpoint
{
    public static void MapStartSessionEndpoint(this WebApplication app)
    {
        app.MapPost("/api/sessions/{id:guid}/start", async (
            [FromRoute] Guid id,
            IMediator mediator,
            IGameNotifier notifier,
            ILogger<Program> logger) =>
        {
            try
            {
                var command = new TransitionSessionCommand(id, "Active");
                await mediator.Send(command);

                await notifier.NotifySessionStatusChanged(id, "Active");

                logger.LogInformation(
                    "Session started successfully: Id={SessionId}",
                    id);

                return Results.Ok(new { id, status = "Active" });
            }
            catch (FluentValidation.ValidationException ex)
            {
                logger.LogWarning(
                    "Start session validation failed: {Message}", ex.Message);

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
                    "Session start failed: {Message}", ex.Message);

                return Results.BadRequest(new
                {
                    error = "Cannot start session",
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Session start failed due to an unexpected error");

                var problem = new
                {
                    statusCode = StatusCodes.Status500InternalServerError,
                    title = "Session start failed",
                    detail = "An unexpected error occurred while starting the session. Please try again later."
                };

                return Results.Problem(
                    problem.title,
                    null,
                    problem.statusCode,
                    problem.title,
                    problem.detail);
            }
        })
        .WithName("StartSession")
        .RequireAuthorization("operator_or_admin");
    }
}
