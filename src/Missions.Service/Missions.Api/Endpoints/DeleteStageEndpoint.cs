using MediatR;
using Microsoft.AspNetCore.Mvc;
using Missions.Application.Missions.Stages;

namespace Missions.Api.Endpoints;

public static class DeleteStageEndpoint
{
    public static void MapDeleteStageEndpoint(this WebApplication app)
    {
        app.MapDelete("/missions/{id:guid}/stages/{stageId:guid}", async (
            [FromRoute] Guid id,
            [FromRoute] Guid stageId,
            IMediator mediator,
            ILogger<Program> logger) =>
        {
            try
            {
                var command = new DeleteStageCommand(id, stageId);
                await mediator.Send(command);

                logger.LogInformation("Stage deleted: Id={StageId}, MissionId={MissionId}", stageId, id);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning("Stage deletion failed: {Message}", ex.Message);
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Stage deletion failed due to an unexpected error");
                return Results.Problem("Stage deletion failed", null, 500, "Stage deletion failed",
                    "An unexpected error occurred while deleting the stage.");
            }
        })
        .WithName("DeleteStage")
        .RequireAuthorization("admin");
    }
}
