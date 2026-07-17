using MediatR;
using Sessions.Application.Sessions.Teams.RemoveMember;

namespace Sessions.Api.Endpoints;

public static class RemoveTeamMemberEndpoint
{
    public static void MapRemoveTeamMemberEndpoint(this WebApplication app)
    {
        app.MapDelete("/{sessionId:guid}/teams/{teamId:guid}/members/{userId:guid}", async (
            Guid sessionId,
            Guid teamId,
            Guid userId,
            IMediator mediator,
            ILogger<Program> logger) =>
        {
            try
            {
                var command = new RemoveTeamMemberCommand(sessionId, teamId, userId);
                await mediator.Send(command);

                logger.LogInformation(
                    "User {UserId} removed from team {TeamId} in session {SessionId}",
                    userId, teamId, sessionId);

                return Results.NoContent();
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("no encontrado") || ex.Message.Contains("not found"))
            {
                logger.LogWarning("Remove member failed: {Message}", ex.Message);
                return Results.NotFound(new { error = "Not found", message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning("Remove member failed: {Message}", ex.Message);
                return Results.BadRequest(new { error = "Cannot remove member", message = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Remove member failed unexpectedly");
                return Results.Problem("Remove member failed", statusCode: 500);
            }
        })
        .WithName("RemoveTeamMember")
        .RequireAuthorization("operator");
    }
}
