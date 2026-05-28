using MediatR;
using Teams.Application.Teams.Update;
using Microsoft.AspNetCore.Mvc;

namespace Teams.Api.Endpoints;

public static class UpdateTeamEndpoint
{
    public static void MapUpdateTeamEndpoint(this WebApplication app)
    {
        app.MapPut("/api/teams/{id:guid}", async (
            [FromRoute] Guid id,
            [FromBody] UpdateTeamCommand command,
            IMediator mediator,
            ILogger<Program> logger) =>
        {
            try
            {
                var updatedCommand = command with { Id = id };
                await mediator.Send(updatedCommand);

                logger.LogInformation(
                    "Team updated successfully: Id={TeamId}",
                    id);

                return Results.NoContent();
            }
            catch (FluentValidation.ValidationException ex)
            {
                logger.LogWarning(
                    "Team update validation failed: {Message}", ex.Message);

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
            catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
            {
                logger.LogWarning(
                    "Team not found: Id={TeamId}", id);

                return Results.NotFound(new
                {
                    error = "Not Found",
                    message = ex.Message
                });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
            {
                logger.LogWarning(
                    "Team update conflict: {Message}", ex.Message);

                return Results.Conflict(new
                {
                    error = "Conflict",
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Team update failed due to an unexpected error");

                var problem = new
                {
                    statusCode = StatusCodes.Status500InternalServerError,
                    title = "Team update failed",
                    detail = "An unexpected error occurred while updating the team. Please try again later."
                };

                return Results.Problem(
                    problem.title,
                    null,
                    problem.statusCode,
                    problem.title,
                    problem.detail);
            }
        })
        .WithName("UpdateTeam")
        .RequireAuthorization("admin");
    }
}
