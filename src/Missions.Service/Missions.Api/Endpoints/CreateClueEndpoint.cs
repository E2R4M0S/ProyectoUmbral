using MediatR;
using Missions.Application.Missions.Clues;
using Microsoft.AspNetCore.Mvc;

namespace Missions.Api.Endpoints;

public static class CreateClueEndpoint
{
    public static void MapCreateClueEndpoint(this WebApplication app)
    {
        app.MapPost("/admin/missions/{id:guid}/stages/{stageId:guid}/clues", async (
            [FromRoute] Guid id,
            [FromRoute] Guid stageId,
            [FromBody] CreateClueCommand command,
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
                var result = await mediator.Send(command);

                logger.LogInformation(
                    "Clue created successfully: Id={ClueId}, StageId={StageId}, MissionId={MissionId}",
                    result.Id, stageId, id);

                return Results.Created(
                    $"/admin/missions/{id}/stages/{stageId}/clues/{result.Id}",
                    new
                    {
                        id = result.Id,
                        content = result.Content,
                        penalty = result.Penalty,
                        releaseType = result.ReleaseType
                    });
            }
            catch (FluentValidation.ValidationException ex)
            {
                logger.LogWarning(
                    "Clue creation validation failed: {Message}", ex.Message);

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
                    "Clue creation failed: Mission not found — {Message}", ex.Message);

                return Results.NotFound(new
                {
                    error = "Not Found",
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Clue creation failed due to an unexpected error");

                var problem = new
                {
                    statusCode = StatusCodes.Status500InternalServerError,
                    title = "Clue creation failed",
                    detail = "An unexpected error occurred while creating the clue. Please try again later."
                };

                return Results.Problem(
                    problem.title,
                    null,
                    problem.statusCode,
                    problem.title,
                    problem.detail);
            }
        })
        .WithName("CreateClue")
        .RequireAuthorization("admin");
    }
}
