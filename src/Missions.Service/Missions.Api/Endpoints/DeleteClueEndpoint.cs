using MediatR;
using Missions.Application.Missions.Clues;
using Microsoft.AspNetCore.Mvc;

namespace Missions.Api.Endpoints;

public static class DeleteClueEndpoint
{
    public static void MapDeleteClueEndpoint(this WebApplication app)
    {
        app.MapDelete("/admin/missions/{id:guid}/stages/{stageId:guid}/clues/{clueId:guid}", async (
            [FromRoute] Guid id,
            [FromRoute] Guid stageId,
            [FromRoute] Guid clueId,
            IMediator mediator,
            ILogger<Program> logger) =>
        {
            var command = new DeleteClueCommand(id, stageId, clueId);

            try
            {
                await mediator.Send(command);

                logger.LogInformation(
                    "Clue deleted successfully: Id={ClueId}, StageId={StageId}, MissionId={MissionId}",
                    clueId, stageId, id);

                return Results.NoContent();
            }
            catch (FluentValidation.ValidationException ex)
            {
                logger.LogWarning(
                    "Clue deletion validation failed: {Message}", ex.Message);

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
                    "Clue deletion failed: Not found — {Message}", ex.Message);

                return Results.NotFound(new
                {
                    error = "Not Found",
                    message = ex.Message
                });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("is in use"))
            {
                logger.LogWarning(
                    "Clue deletion conflict: Mission in use — {Message}", ex.Message);

                return Results.Conflict(new
                {
                    error = "Conflict",
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Clue deletion failed due to an unexpected error");

                var problem = new
                {
                    statusCode = StatusCodes.Status500InternalServerError,
                    title = "Clue deletion failed",
                    detail = "An unexpected error occurred while deleting the clue. Please try again later."
                };

                return Results.Problem(
                    problem.title,
                    null,
                    problem.statusCode,
                    problem.title,
                    problem.detail);
            }
        })
        .WithName("DeleteClue")
        .RequireAuthorization("admin");
    }
}
