using MediatR;
using Sessions.Application.Common.Interfaces;
using Sessions.Application.Sessions.Transition;
using Microsoft.AspNetCore.Mvc;

namespace Sessions.Api.Endpoints;

public static class TransitionSessionEndpoint
{
    public static void MapTransitionSessionEndpoint(this WebApplication app)
    {
        app.MapPatch("/{id:guid}/status", async (
            [FromRoute] Guid id,
            [FromBody] TransitionSessionCommand command,
            IGameSessionFacade facade,
            ILogger<Program> logger) =>
        {
            try
            {
                var updatedCommand = command with { Id = id };
                await facade.TransitionAndNotify(id, command.NewStatus);

                logger.LogInformation(
                    "Session status transitioned: Id={SessionId}, Status={Status}",
                    id, command.NewStatus);

                return Results.NoContent();
            }
            catch (FluentValidation.ValidationException ex)
            {
                logger.LogWarning("Session transition validation failed: {Message}", ex.Message);
                return Results.BadRequest(new
                {
                    error = "Validation failed",
                    details = ex.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage })
                });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
            {
                logger.LogWarning("Session not found: Id={SessionId}", id);
                return Results.NotFound(new { error = "Not Found", message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning("Invalid session status transition: {Message}", ex.Message);
                return Results.BadRequest(new { error = "Invalid status transition", message = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Session status transition failed");
                return Results.Problem("Session status transition failed", null, 500, "Transition failed", "An unexpected error occurred.");
            }
        })
        .WithName("TransitionSession")
        .RequireAuthorization("operator_or_admin");
    }
}
