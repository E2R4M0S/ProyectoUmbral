using MediatR;
using Sessions.Application.Sessions.Consult;
using Microsoft.AspNetCore.Mvc;

namespace Sessions.Api.Endpoints;

public static class GetAuditLogEndpoint
{
    public static void MapGetAuditLogEndpoint(this WebApplication app)
    {
        app.MapGet("/{id:guid}/audit-log", async (
            [FromRoute] Guid id,
            IMediator mediator,
            ILogger<Program> logger) =>
        {
            try
            {
                var events = await mediator.Send(new GetAuditLogQuery(id));

                if (events is null)
                {
                    return Results.NotFound(new { error = "Not Found", message = $"Session with id '{id}' not found" });
                }

                return Results.Ok(events);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get session audit log: Id={SessionId}", id);

                return Results.Problem(
                    "Failed to get session audit log",
                    null,
                    StatusCodes.Status500InternalServerError,
                    "GetAuditLog failed",
                    "An unexpected error occurred while getting the session audit log.");
            }
        })
        .WithName("GetSessionAuditLog")
        .RequireAuthorization("operator_or_admin");
    }
}
