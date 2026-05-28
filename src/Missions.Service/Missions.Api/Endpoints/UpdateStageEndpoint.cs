using MediatR;
using Missions.Application.Missions.Stages;
using Microsoft.AspNetCore.Mvc;

namespace Missions.Api.Endpoints;

public static class UpdateStageEndpoint
{
    public static void MapUpdateStageEndpoint(this WebApplication app)
    {
        app.MapPut("/admin/missions/{id:guid}/stages/{stageId:guid}", async (
            [FromRoute] Guid id,
            [FromRoute] Guid stageId,
            [FromBody] UpdateStageCommand command,
            IMediator mediator,
            ILogger<Program> logger) =>
        {
            if (id != command.MissionId)
            {
                return Results.BadRequest(new
                {
                    error = "Validation failed",
                    details = new[]
                    {
                        new { field = "MissionId", message = "MissionId in body must match route id" }
                    }
                });
            }

            if (stageId != command.StageId)
            {
                return Results.BadRequest(new
                {
                    error = "Validation failed",
                    details = new[]
                    {
                        new { field = "StageId", message = "StageId in body must match route stageId" }
                    }
                });
            }

            try
            {
                var updatedCommand = command with { MissionId = id, StageId = stageId };
                await mediator.Send(updatedCommand);

                logger.LogInformation(
                    "Stage updated successfully: Id={StageId}, MissionId={MissionId}",
                    stageId, id);

                return Results.NoContent();
            }
            catch (FluentValidation.ValidationException ex)
            {
                logger.LogWarning(
                    "Stage update validation failed: {Message}", ex.Message);

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
                    "Stage update failed: {Message}", ex.Message);

                return Results.NotFound(new
                {
                    error = "Not Found",
                    message = ex.Message
                });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already exists") || ex.Message.Contains("is in use"))
            {
                logger.LogWarning(
                    "Stage update conflict: {Message}", ex.Message);

                return Results.Conflict(new
                {
                    error = "Conflict",
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Stage update failed due to an unexpected error");

                var problem = new
                {
                    statusCode = StatusCodes.Status500InternalServerError,
                    title = "Stage update failed",
                    detail = "An unexpected error occurred while updating the stage. Please try again later."
                };

                return Results.Problem(
                    problem.title,
                    null,
                    problem.statusCode,
                    problem.title,
                    problem.detail);
            }
        })
        .WithName("UpdateStage")
        .RequireAuthorization("admin");
    }
}
