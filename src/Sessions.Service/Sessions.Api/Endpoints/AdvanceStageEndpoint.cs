using Sessions.Application.Common;
using Sessions.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Sessions.Domain.Enums;
using System.Linq;

namespace Sessions.Api.Endpoints;

public static class AdvanceStageEndpoint
{
    public static void MapAdvanceStageEndpoint(this WebApplication app)
    {
        app.MapPatch("/{id:guid}/advance-stage", async (
            [FromRoute] Guid id,
            HttpContext httpContext,
            ISessionRepository repository,
            IGameSessionFacade facade,
            ILogger<Program> logger) =>
        {
            try
            {
                var session = await repository.GetByIdWithStagesAsync(id, CancellationToken.None);
                if (session is null)
                {
                    return Results.NotFound(new { error = "Not Found", message = $"Session with id '{id}' not found" });
                }

                // RB-10: only the operator who created this session may advance it.
                var currentUserId = CurrentUserClaims.GetUserId(httpContext.User);
                if (!session.IsManagedBy(currentUserId))
                {
                    return Results.Json(
                        new { error = "Forbidden", message = "Solo el operador que creó esta sesión puede administrarla" },
                        statusCode: StatusCodes.Status403Forbidden);
                }

                if (session.Status != SessionStatus.Active)
                {
                    return Results.BadRequest(new
                    {
                        error = "Session not active",
                        message = $"Cannot advance stage: session is in '{session.Status}' status (must be Active)"
                    });
                }

                var newOrder = session.CurrentStageOrder + 1;
                if (newOrder >= session.Stages.Count)
                {
                    return Results.BadRequest(new
                    {
                        error = "Already on last stage",
                        message = "Cannot advance stage: already on the last stage"
                    });
                }

                session.AdvanceStage();
                await repository.UpdateAsync(session, CancellationToken.None);

                // Catch up any participant still lagging behind (e.g. stuck on a Trivia stage,
                // which has no QR to scan) so their per-participant stage pointer used for QR
                // validation stays in sync with the session's now-current stage.
                foreach (var participant in session.Participants.Where(p => p.CurrentStageOrder < newOrder))
                {
                    participant.CatchUpTo(newOrder);
                    await repository.UpdateParticipantAsync(participant, CancellationToken.None);
                }

                await facade.NotifyStageAdvanced(id, session.CurrentStageOrder, CancellationToken.None);

                logger.LogInformation(
                    "Session stage advanced: Id={SessionId}, CurrentStageOrder={Order}/{Total}",
                    id, session.CurrentStageOrder, session.Stages.Count);

                return Results.Ok(new
                {
                    currentStageOrder = session.CurrentStageOrder,
                    totalStages = session.Stages.Count,
                    isLastStage = session.CurrentStageOrder >= session.Stages.Count - 1
                });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
            {
                return Results.NotFound(new { error = "Not Found", message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning("Advance stage failed: {Message}", ex.Message);
                return Results.BadRequest(new { error = "Cannot advance stage", message = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Advance stage failed due to an unexpected error");
                return Results.Problem("Advance stage failed", null, 500, "AdvanceStage failed", "An unexpected error occurred.");
            }
        })
        .WithName("AdvanceStage")
        .RequireAuthorization("operator");
    }
}
