using MediatR;
using Sessions.Application.Sessions.Consult;

namespace Sessions.Api.Endpoints;

public static class GetMySessionsEndpoint
{
    public static void MapGetMySessionsEndpoint(this WebApplication app)
    {
        app.MapGet("/participants/me/sessions", async (
            IMediator mediator,
            ILogger<Program> logger) =>
        {
            try
            {
                var sessions = await mediator.Send(new GetMySessionsQuery());
                return Results.Ok(sessions);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get participant's session catalog");
                return Results.Problem(
                    "Failed to get session catalog",
                    null,
                    StatusCodes.Status500InternalServerError,
                    "GetMySessions failed",
                    "An unexpected error occurred while getting your session catalog.");
            }
        })
        .WithName("GetMySessions")
        .RequireAuthorization();
    }
}
