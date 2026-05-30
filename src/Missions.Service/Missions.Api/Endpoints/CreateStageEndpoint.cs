using MediatR;
using Missions.Application.Missions.Stages;
using Microsoft.AspNetCore.Mvc;

namespace Missions.Api.Endpoints;

public static class CreateStageEndpoint
{
    public static void MapCreateStageEndpoint(this WebApplication app)
    {
        app.MapPost("/missions/{id:guid}/stages", async (
            [FromRoute] Guid id,
            [FromBody] CreateStageCommand command,
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

            try
            {
                var result = await mediator.Send(command);

                logger.LogInformation(
                    "Stage created successfully: Id={StageId}, MissionId={MissionId}",
                    result.Id, id);

                return Results.Created(
                    $"/missions/{id}/stages/{result.Id}",
                    new
                    {
                        id = result.Id,
                        missionId = id,
                        name = result.Name,
                        description = result.Description,
                        order = result.Order
                    });
            }
            catch (FluentValidation.ValidationException ex)
            {
                logger.LogWarning(
                    "Stage creation validation failed: {Message}", ex.Message);

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
                    "Stage creation failed: Mission not found — {Message}", ex.Message);

                return Results.NotFound(new
                {
                    error = "Not Found",
                    message = ex.Message
                });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already exists") || ex.Message.Contains("Ya existe"))
            {
                logger.LogWarning(
                    "Stage creation conflict: Order already exists — {Message}", ex.Message);

                return Results.Conflict(new
                {
                    error = "Conflict",
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Stage creation failed due to an unexpected error");

                var problem = new
                {
                    statusCode = StatusCodes.Status500InternalServerError,
                    title = "Stage creation failed",
                    detail = "An unexpected error occurred while creating the stage. Please try again later."
                };

                return Results.Problem(
                    problem.title,
                    null,
                    problem.statusCode,
                    problem.title,
                    problem.detail);
            }
        })
        .WithName("CreateStage")
        .RequireAuthorization("admin");
    }
}
