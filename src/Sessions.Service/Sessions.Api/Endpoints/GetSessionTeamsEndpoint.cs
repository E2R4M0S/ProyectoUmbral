using MediatR;
using Sessions.Application.Sessions.Teams.GetTeams;

namespace Sessions.Api.Endpoints;

public static class GetSessionTeamsEndpoint
{
    public static void MapGetSessionTeamsEndpoint(this WebApplication app)
    {
        app.MapGet("/{sessionId:guid}/teams", async (
            Guid sessionId,
            IMediator mediator,
            ILogger<Program> logger) =>
        {
            try
            {
                var result = await mediator.Send(new GetSessionTeamsQuery(sessionId));
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Get session teams failed for session {SessionId}", sessionId);
                return Results.Problem("Get teams failed", statusCode: 500);
            }
        })
        .WithName("GetSessionTeams")
        .RequireAuthorization();
    }
}
