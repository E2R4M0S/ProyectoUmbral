using System.Net.Http.Json;
using System.Text.Json;
using Sessions.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Sessions.Api.Endpoints;

public static class ReleaseClueEndpoint
{
    public static void MapReleaseClueEndpoint(this WebApplication app)
    {
        app.MapPost("/{id:guid}/clues/release", async (
            [FromRoute] Guid id,
            [FromBody] ReleaseClueRequest request,
            IGameSessionFacade facade,
            ISessionRepository sessionRepo,
            IHttpClientFactory httpClientFactory,
            ILogger<Program> logger) =>
        {
            try
            {
                // 1. Get session to find MissionId
                var session = await sessionRepo.GetByIdAsync(id, CancellationToken.None);
                if (session is null)
                    return Results.NotFound(new { error = "Session not found" });

                // 2. Fetch mission detail from Missions.Service
                var httpClient = httpClientFactory.CreateClient("MissionsClient");
                var missionResponse = await httpClient.GetAsync($"/{session.MissionId}", CancellationToken.None);

                string? clueContent = null;
                int? cluePenalty = null;

                if (missionResponse.IsSuccessStatusCode)
                {
                    var mission = await missionResponse.Content.ReadFromJsonAsync<JsonElement>();
                    if (mission.TryGetProperty("stages", out var stages))
                    {
                        foreach (var stage in stages.EnumerateArray())
                        {
                            if (stage.TryGetProperty("clues", out var clues))
                            {
                                foreach (var clue in clues.EnumerateArray())
                                {
                                    if (clue.TryGetProperty("id", out var cid) &&
                                        cid.GetGuid() == request.ClueId)
                                    {
                                        clueContent = clue.TryGetProperty("content", out var c) ? c.GetString() : null;
                                        cluePenalty = clue.TryGetProperty("penalty", out var p) && p.ValueKind != JsonValueKind.Null ? p.GetInt32() : null;
                                        break;
                                    }
                                }
                            }
                            if (clueContent is not null) break;
                        }
                    }
                }

                // 3. Send real clue data via SignalR through Facade
                await facade.ReleaseClueAndNotify(id, request.ClueId, request.TeamId, clueContent, cluePenalty);

                logger.LogInformation(
                    "Clue released: SessionId={SessionId}, ClueId={ClueId}, HasContent={HasContent}",
                    id, request.ClueId, clueContent is not null);

                return Results.Ok(new { id, clueId = request.ClueId, status = "Released", hasContent = clueContent is not null });
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
        .RequireAuthorization("operator_or_admin");
    }
}

public record ReleaseClueRequest(Guid ClueId, Guid? TeamId = null);
