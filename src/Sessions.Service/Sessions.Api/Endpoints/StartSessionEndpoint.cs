using Sessions.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Sessions.Api.Endpoints;

public static class StartSessionEndpoint
{
    public static void MapStartSessionEndpoint(this WebApplication app)
    {
        app.MapPost("/{id:guid}/start", async (
            [FromRoute] Guid id,
            IGameSessionFacade facade,
            ILogger<Program> logger) =>
        {
            try
            {
                await facade.TransitionAndNotify(id, "Active");
                logger.LogInformation("Session started: Id={SessionId}", id);
                return Results.Ok(new { id, status = "Active" });
            }
            catch (FluentValidation.ValidationException ex)
            {
                logger.LogWarning("Start session validation failed: {Message}", ex.Message);
                return Results.BadRequest(new { error = "Validation failed", details = ex.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage }) });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
            {
                logger.LogWarning("Session not found: Id={SessionId}", id);
                return Results.NotFound(new { error = "Not Found", message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning("Session start failed: {Message}", ex.Message);
                return Results.BadRequest(new { error = "Cannot start session", message = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Session start failed");
                return Results.Problem("Session start failed", null, 500, "Start failed", "An unexpected error occurred.");
            }
        })
        .WithName("StartSession")
        .RequireAuthorization("operator_or_admin");
    }
}
