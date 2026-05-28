using MediatR;
using Teams.Application.Teams.Join;
using Microsoft.AspNetCore.Mvc;

namespace Teams.Api.Endpoints;

public static class JoinTeamEndpoint
{
    public static void MapJoinTeamEndpoint(this WebApplication app)
    {
        app.MapPost("/api/teams/{id:guid}/join", async (
            [FromRoute] Guid id,
            [FromBody] JoinTeamRequest request,
            IMediator mediator,
            ILogger<Program> logger) =>
        {
            try
            {
                var command = new JoinTeamCommand(id, request.JoinCode);
                var result = await mediator.Send(command);

                logger.LogInformation(
                    "User joined team successfully: TeamId={TeamId}",
                    id);

                return Results.NoContent();
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
            {
                logger.LogWarning(
                    "Team not found: TeamId={TeamId}", id);

                return Results.NotFound(new
                {
                    error = "Not Found",
                    message = ex.Message
                });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Invalid join code"))
            {
                logger.LogWarning(
                    "Invalid join code for team: TeamId={TeamId}", id);

                return Results.Json(
                    new { error = "Forbidden", message = ex.Message },
                    statusCode: StatusCodes.Status403Forbidden);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already a member"))
            {
                logger.LogWarning(
                    "User already a member of team: TeamId={TeamId}", id);

                return Results.Conflict(new
                {
                    error = "Conflict",
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Join team failed due to an unexpected error");

                var problem = new
                {
                    statusCode = StatusCodes.Status500InternalServerError,
                    title = "Join team failed",
                    detail = "An unexpected error occurred while joining the team. Please try again later."
                };

                return Results.Problem(
                    problem.title,
                    null,
                    problem.statusCode,
                    problem.title,
                    problem.detail);
            }
        })
        .WithName("JoinTeam")
        .RequireAuthorization("authenticated");
    }
}

public record JoinTeamRequest(string JoinCode);
