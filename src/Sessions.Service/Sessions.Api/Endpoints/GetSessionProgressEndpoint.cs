using MediatR;
using Sessions.Application.Common.Interfaces;
using Sessions.Application.Sessions.Consult;
using Microsoft.AspNetCore.Mvc;

namespace Sessions.Api.Endpoints;

public static class GetSessionProgressEndpoint
{
    public static void MapGetSessionProgressEndpoint(this WebApplication app)
    {
        app.MapGet("/api/sessions/{id:guid}/progress", async (
            [FromRoute] Guid id,
            IMediator mediator,
            ILogger<Program> logger) =>
        {
            try
            {
                var progress = await mediator.Send(new GetSessionProgressQuery(id));

                if (progress is null)
                {
                    return Results.NotFound(new { error = "Not Found", message = $"Session with id '{id}' not found" });
                }

                return Results.Ok(progress);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get session progress: Id={SessionId}", id);

                return Results.Problem(
                    "Failed to get session progress",
                    null,
                    StatusCodes.Status500InternalServerError,
                    "GetSessionProgress failed",
                    "An unexpected error occurred while getting session progress.");
            }
        })
        .WithName("GetSessionProgress")
        .RequireAuthorization("operator_or_admin");
    }
}
