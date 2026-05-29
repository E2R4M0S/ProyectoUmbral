using MediatR;
using Teams.Application.Teams.Join;
using Microsoft.AspNetCore.Mvc;

namespace Teams.Api.Endpoints;

public static class JoinTeamEndpoint
{
    public static void MapJoinTeamEndpoint(this WebApplication app)
    {
        app.MapPost("/api/teams/join", async (
            [FromBody] JoinTeamRequest request,
            IMediator mediator,
            ILogger<Program> logger) =>
        {
            try
            {
                var command = new JoinTeamCommand(request.JoinCode);
                var result = await mediator.Send(command);

                logger.LogInformation(
                    "User joined team successfully");

                return Results.NoContent();
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("not found") || ex.Message.Contains("Invalid join code"))
            {
                logger.LogWarning(
                    "Join team failed: {Message}", ex.Message);

                return Results.Json(
                    new { error = "Forbidden", message = ex.Message },
                    statusCode: StatusCodes.Status403Forbidden);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already a member"))
            {
                logger.LogWarning(
                    "Join team failed: already a member");

                return Results.Conflict(new
                {
                    error = "Conflict",
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Join team failed due to an unexpected error");

                return Results.Problem(
                    "Join team failed",
                    null,
                    StatusCodes.Status500InternalServerError,
                    "Join team failed",
                    "An unexpected error occurred while joining the team.");
            }
        })
        .WithName("JoinTeam")
        .RequireAuthorization("authenticated");
    }
}

public record JoinTeamRequest(string JoinCode);
