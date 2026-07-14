using MediatR;
using Sessions.Application.Sessions.Teams.JoinTeam;

namespace Sessions.Api.Endpoints;

public static class JoinSessionTeamEndpoint
{
    public static void MapJoinSessionTeamEndpoint(this WebApplication app)
    {
        app.MapPost("/{sessionId:guid}/teams/{teamId:guid}/join", async (
            Guid sessionId,
            Guid teamId,
            IMediator mediator,
            ILogger<Program> logger) =>
        {
            try
            {
                var command = new JoinSessionTeamCommand(sessionId, teamId);
                var result = await mediator.Send(command);

                logger.LogInformation(
                    "User {UserId} joined team {TeamId} in session {SessionId}",
                    result.UserId, result.TeamId, sessionId);

                return Results.Ok(new
                {
                    teamId = result.TeamId,
                    teamName = result.TeamName,
                    userId = result.UserId,
                    userAlias = result.UserAlias
                });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("no encontrada") || ex.Message.Contains("not found"))
            {
                logger.LogWarning("Join team failed: {Message}", ex.Message);
                return Results.NotFound(new { error = "Not found", message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning("Join team failed: {Message}", ex.Message);
                return Results.BadRequest(new { error = "Cannot join team", message = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Join team failed unexpectedly");
                return Results.Problem("Join team failed", statusCode: 500);
            }
        })
        .WithName("JoinSessionTeam")
        .RequireAuthorization();
    }
}
