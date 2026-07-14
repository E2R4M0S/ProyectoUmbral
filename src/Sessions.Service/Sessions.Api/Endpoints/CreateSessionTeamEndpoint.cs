using MediatR;
using Microsoft.AspNetCore.Mvc;
using Sessions.Application.Sessions.Teams.CreateTeam;

namespace Sessions.Api.Endpoints;

public static class CreateSessionTeamEndpoint
{
    public static void MapCreateSessionTeamEndpoint(this WebApplication app)
    {
        app.MapPost("/{sessionId:guid}/teams", async (
            Guid sessionId,
            [FromBody] CreateTeamBody body,
            IMediator mediator,
            ILogger<Program> logger) =>
        {
            try
            {
                var command = new CreateSessionTeamCommand(sessionId, body.Name);
                var result = await mediator.Send(command);

                logger.LogInformation(
                    "Team created: Id={TeamId}, Name={Name}, SessionId={SessionId}",
                    result.TeamId, result.Name, sessionId);

                return Results.Created(
                    $"/{sessionId}/teams/{result.TeamId}",
                    new { id = result.TeamId, name = result.Name, maxMembers = result.MaxMembers, createdAt = result.CreatedAt });
            }
            catch (FluentValidation.ValidationException ex)
            {
                return Results.BadRequest(new
                {
                    error = "Validation failed",
                    details = ex.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage })
                });
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning("Create team failed: {Message}", ex.Message);
                return Results.BadRequest(new { error = "Cannot create team", message = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Create team failed unexpectedly");
                return Results.Problem("Create team failed", statusCode: 500);
            }
        })
        .WithName("CreateSessionTeam")
        .RequireAuthorization("operator_or_admin");
    }

    public record CreateTeamBody(string Name);
}
