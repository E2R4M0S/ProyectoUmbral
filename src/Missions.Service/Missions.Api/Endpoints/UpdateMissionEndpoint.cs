using MediatR;
using Missions.Application.Missions.Update;
using Microsoft.AspNetCore.Mvc;

namespace Missions.Api.Endpoints;

public static class UpdateMissionEndpoint
{
    public static void MapUpdateMissionEndpoint(this WebApplication app)
    {
        app.MapPut("/missions/{id:guid}", async (
            [FromRoute] Guid id,
            [FromBody] UpdateMissionCommand command,
            IMediator mediator,
            ILogger<Program> logger) =>
        {
            try
            {
                var updatedCommand = command with { Id = id };
                await mediator.Send(updatedCommand);

                logger.LogInformation(
                    "Mission updated successfully: Id={MissionId}",
                    id);

                return Results.NoContent();
            }
            catch (FluentValidation.ValidationException ex)
            {
                logger.LogWarning(
                    "Mission update validation failed: {Message}", ex.Message);

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
            catch (InvalidOperationException ex) when (ex.Message.Contains("already exists") || ex.Message.Contains("is in use"))
            {
                logger.LogWarning(
                    "Mission update conflict: {Message}", ex.Message);

                return Results.Conflict(new
                {
                    error = "Conflict",
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Mission update failed due to an unexpected error");

                var problem = new
                {
                    statusCode = StatusCodes.Status500InternalServerError,
                    title = "Mission update failed",
                    detail = "An unexpected error occurred while updating the mission. Please try again later."
                };

                return Results.Problem(
                    problem.title,
                    null,
                    problem.statusCode,
                    problem.title,
                    problem.detail);
            }
        })
        .WithName("UpdateMission")
        .RequireAuthorization("admin");
    }
}
