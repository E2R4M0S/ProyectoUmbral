using MediatR;
using Missions.Application.Missions.StatusChange;
using Microsoft.AspNetCore.Mvc;

namespace Missions.Api.Endpoints;

public static class ChangeMissionStatusEndpoint
{
    public static void MapChangeMissionStatusEndpoint(this WebApplication app)
    {
        app.MapPatch("/missions/{id:guid}/status", async (
            [FromRoute] Guid id,
            [FromBody] ChangeMissionStatusCommand command,
            IMediator mediator,
            ILogger<Program> logger) =>
        {
            try
            {
                var updatedCommand = command with { Id = id };
                await mediator.Send(updatedCommand);

                logger.LogInformation(
                    "Mission status changed successfully: Id={MissionId}",
                    id);

                return Results.NoContent();
            }
            catch (FluentValidation.ValidationException ex)
            {
                logger.LogWarning(
                    "Mission status change validation failed: {Message}", ex.Message);

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
                    "Mission not found: Id={MissionId}", id);

                return Results.NotFound(new
                {
                    error = "Not Found",
                    message = ex.Message
                });
            }
            catch (InvalidOperationException ex) when (
                ex.Message.Contains("Cannot transition") ||
                ex.Message.Contains("already in"))
            {
                logger.LogWarning(
                    "Invalid mission status transition: {Message}", ex.Message);

                return Results.BadRequest(new
                {
                    error = "Invalid status transition",
                    message = ex.Message
                });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("in use") || ex.Message.Contains("at least one stage") || ex.Message.Contains("sin etapas"))
            {
                logger.LogWarning(
                    "Mission status change conflict: {Message}", ex.Message);

                return Results.Conflict(new
                {
                    error = "Conflict",
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Mission status change failed due to an unexpected error");

                var problem = new
                {
                    statusCode = StatusCodes.Status500InternalServerError,
                    title = "Mission status change failed",
                    detail = "An unexpected error occurred while changing mission status. Please try again later."
                };

                return Results.Problem(
                    problem.title,
                    null,
                    problem.statusCode,
                    problem.title,
                    problem.detail);
            }
        })
        .WithName("ChangeMissionStatus")
        .RequireAuthorization("admin");
    }
}