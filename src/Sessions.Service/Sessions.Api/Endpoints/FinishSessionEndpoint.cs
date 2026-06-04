using Sessions.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Sessions.Api.Endpoints;

public static class FinishSessionEndpoint
{
    public static void MapFinishSessionEndpoint(this WebApplication app)
    {
        app.MapPost("/{id:guid}/finish", async (
            [FromRoute] Guid id,
            IGameSessionFacade facade,
            ILogger<Program> logger) =>
        {
            try
            {
                await facade.TransitionAndNotify(id, "Finished");
                logger.LogInformation("Session finished: Id={SessionId}", id);
                return Results.Ok(new { id, status = "Finished" });
            }
            catch (FluentValidation.ValidationException ex)
            {
                logger.LogWarning("Finish session validation failed: {Message}", ex.Message);
                return Results.BadRequest(new { error = "Validation failed", details = ex.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage }) });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
            {
                logger.LogWarning("Session not found: Id={SessionId}", id);
                return Results.NotFound(new { error = "Not Found", message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning("Session finish failed: {Message}", ex.Message);
                return Results.BadRequest(new { error = "Cannot finish session", message = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Session finish failed");
                return Results.Problem("Session finish failed", null, 500, "Finish failed", "An unexpected error occurred.");
            }
        })
        .WithName("FinishSession")
        .RequireAuthorization("operator_or_admin");
    }
}
