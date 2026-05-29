using MediatR;
using Sessions.Application.Common.Interfaces;
using Sessions.Application.Sessions.Clues;
using Microsoft.AspNetCore.Mvc;

namespace Sessions.Api.Endpoints;

public static class ReleaseClueEndpoint
{
    public static void MapReleaseClueEndpoint(this WebApplication app)
    {
        app.MapPost("/api/sessions/{id:guid}/clues/release", async (
            [FromRoute] Guid id,
            [FromBody] ReleaseClueRequest request,
            IMediator mediator,
            IGameNotifier notifier,
            ILogger<Program> logger) =>
        {
            try
            {
                var command = new ReleaseClueCommand(id, request.ClueId, request.TeamId);
                await mediator.Send(command);

                logger.LogInformation(
                    "Clue released successfully: SessionId={SessionId}, ClueId={ClueId}",
                    id, request.ClueId);

                return Results.Ok(new { id, clueId = request.ClueId, status = "Released" });
            }
            catch (FluentValidation.ValidationException ex)
            {
                logger.LogWarning(
                    "Release clue validation failed: {Message}", ex.Message);

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
            catch (Exception ex)
            {
                logger.LogError(ex, "Clue release failed due to an unexpected error");

                return Results.Problem(
                    "Clue release failed",
                    null,
                    StatusCodes.Status500InternalServerError,
                    "ReleaseClue failed",
                    "An unexpected error occurred while releasing the clue.");
            }
        })
        .WithName("ReleaseClue")
        .RequireAuthorization("operator_or_admin");
    }
}

public record ReleaseClueRequest(Guid ClueId, Guid? TeamId = null);
