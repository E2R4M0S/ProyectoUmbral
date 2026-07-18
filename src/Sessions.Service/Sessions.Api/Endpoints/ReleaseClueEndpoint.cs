using System.Net.Http.Json;
using System.Text.Json;
using Sessions.Application.Common;
using Sessions.Application.Common.Interfaces;
using Sessions.Domain.Entities;
using Sessions.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Sessions.Api.Endpoints;

public static class ReleaseClueEndpoint
{
    public static void MapReleaseClueEndpoint(this WebApplication app)
    {
        app.MapPost("/{id:guid}/clues/release", async (
            [FromRoute] Guid id,
            [FromBody] ReleaseClueRequest request,
            HttpContext httpContext,
            IGameSessionFacade facade,
            ISessionRepository sessionRepo,
            IGameNotifier notifier,
            IHttpClientFactory httpClientFactory,
            ILogger<Program> logger) =>
        {
            try
            {
                // 1. Get session to find the current stage's mission
                var session = await sessionRepo.GetByIdWithStagesAsync(id, CancellationToken.None);
                if (session is null)
                    return Results.NotFound(new { error = "Session not found" });

                // RB-03 / HU-34: once a session is terminal, its score/penalties must stay frozen.
                if (session.Status is SessionStatus.Finished or SessionStatus.Cancelled)
                {
                    return Results.Json(
                        new { error = "Session is terminal", message = "No se pueden liberar pistas en una sesión Finalizada o Cancelada" },
                        statusCode: StatusCodes.Status409Conflict);
                }

                // RB-10: only the operator who created this session may release its clues.
                var currentUserId = CurrentUserClaims.GetUserId(httpContext.User);
                if (!session.IsManagedBy(currentUserId))
                {
                    return Results.Json(
                        new { error = "Forbidden", message = "Solo el operador que creó esta sesión puede administrarla" },
                        statusCode: StatusCodes.Status403Forbidden);
                }

                var currentStage = session.GetCurrentStage();
                if (currentStage is null)
                    return Results.BadRequest(new { error = "No active stage", message = "Session has no current stage" });

                // Clues only make sense for Treasure hunts — Trivia has no location-based hints to give.
                if (currentStage.MissionType != "Treasure")
                    return Results.BadRequest(new { error = "Not a Treasure stage", message = "Clues can only be released during a Treasure stage" });

                if (request.TeamId is not null && request.UserId is not null)
                    return Results.BadRequest(new { error = "Invalid target", message = "Provide at most one of teamId or userId, not both" });

                Guid clueId;
                string? clueContent;
                int? cluePenalty;

                if (request.ClueId is { } predefinedClueId)
                {
                    // 2. Fetch mission detail from Missions.Service and look the clue up ONLY
                    //    within the session's current stage — a clue from a different stage of
                    //    the same mission must not be releasable early.
                    var httpClient = httpClientFactory.CreateClient("MissionsClient");
                    var missionResponse = await httpClient.GetAsync($"/{currentStage.MissionId}", CancellationToken.None);

                    clueId = predefinedClueId;
                    clueContent = null;
                    cluePenalty = null;

                    if (missionResponse.IsSuccessStatusCode)
                    {
                        var mission = await missionResponse.Content.ReadFromJsonAsync<JsonElement>();
                        if (mission.TryGetProperty("stages", out var stages))
                        {
                            foreach (var stage in stages.EnumerateArray())
                            {
                                if (!stage.TryGetProperty("id", out var stageId) ||
                                    stageId.GetGuid() != currentStage.MissionStageId)
                                    continue;

                                if (stage.TryGetProperty("clues", out var clues))
                                {
                                    foreach (var clue in clues.EnumerateArray())
                                    {
                                        if (clue.TryGetProperty("id", out var cid) &&
                                            cid.GetGuid() == predefinedClueId)
                                        {
                                            clueContent = clue.TryGetProperty("content", out var c) ? c.GetString() : null;
                                            cluePenalty = clue.TryGetProperty("penalty", out var p) && p.ValueKind != JsonValueKind.Null ? p.GetInt32() : null;
                                        }
                                    }
                                }
                                break;
                            }
                        }
                    }

                    if (clueContent is null)
                        return Results.BadRequest(new { error = "Clue not found", message = "That clue does not belong to the session's current stage" });

                    // RB-04: a predefined clue must not be released twice to the same recipient
                    // (team, individual, or everyone) for the same stage.
                    var auditTrail = await sessionRepo.GetAuditTrailAsync(id, CancellationToken.None);
                    var alreadyReleased = auditTrail.Any(e =>
                        e.EventType == SessionAuditEventTypes.ManualClueReleased &&
                        e.ClueId == predefinedClueId &&
                        e.TeamId == request.TeamId &&
                        e.UserId == request.UserId);
                    if (alreadyReleased)
                    {
                        return Results.Json(
                            new { error = "Clue already released", message = "Esta pista ya fue liberada a este destinatario para esta etapa" },
                            statusCode: StatusCodes.Status409Conflict);
                    }
                }
                else if (!string.IsNullOrWhiteSpace(request.Content))
                {
                    // Ad-hoc clue created live by the operator — not a predefined MissionClue,
                    // so it's broadcast directly without any Missions.Service lookup.
                    clueId = Guid.NewGuid();
                    clueContent = request.Content.Trim();
                    cluePenalty = request.Penalty;
                }
                else
                {
                    return Results.BadRequest(new { error = "Missing clue", message = "Provide either clueId or content" });
                }

                // 3. Send real clue data via SignalR through Facade
                await facade.ReleaseClueAndNotify(id, clueId, request.TeamId, request.UserId, clueContent, cluePenalty);

                // RB-04: record the release so a repeat request for the same predefined clue +
                // recipient is rejected above. Ad-hoc clues always get a fresh Guid, so they can
                // never collide — no need to track them here.
                if (request.ClueId is not null)
                {
                    await sessionRepo.AddAuditEventAsync(SessionAuditEvent.Create(
                        id, SessionAuditEventTypes.ManualClueReleased,
                        $"Pista liberada manualmente: {clueContent}",
                        teamId: request.TeamId, userId: request.UserId, clueId: clueId), CancellationToken.None);
                }

                // 4. Apply the penalty (if any) and let everyone see the updated score right away.
                if (cluePenalty is { } penaltyAmount && penaltyAmount > 0)
                {
                    var penaltyReason = string.IsNullOrWhiteSpace(request.Reason)
                        ? $"Pista liberada: {clueContent}"
                        : request.Reason.Trim();
                    await sessionRepo.ApplyCluePenaltyAsync(id, request.TeamId, request.UserId, penaltyAmount, penaltyReason, CancellationToken.None);
                    var rankingAfterPenalty = await sessionRepo.GetSessionRankingAsync(id, CancellationToken.None);
                    await notifier.NotifySessionRankingUpdatedAsync(id, rankingAfterPenalty, CancellationToken.None);
                }

                logger.LogInformation(
                    "Clue released: SessionId={SessionId}, ClueId={ClueId}, HasContent={HasContent}, Penalty={Penalty}",
                    id, clueId, clueContent is not null, cluePenalty);

                return Results.Ok(new { id, clueId, status = "Released", hasContent = clueContent is not null });
            }
            catch (FluentValidation.ValidationException ex)
            {
                logger.LogWarning("Release clue validation failed: {Message}", ex.Message);
                return Results.BadRequest(new
                {
                    error = "Validation failed",
                    details = ex.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage })
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Clue release failed due to an unexpected error");
                return Results.Problem("Clue release failed", null, StatusCodes.Status500InternalServerError, "ReleaseClue failed", "An unexpected error occurred while releasing the clue.");
            }
        })
        .WithName("ReleaseClue")
        .RequireAuthorization("operator");
    }
}

public record ReleaseClueRequest(Guid? ClueId = null, Guid? TeamId = null, Guid? UserId = null, string? Content = null, int? Penalty = null, string? Reason = null);
