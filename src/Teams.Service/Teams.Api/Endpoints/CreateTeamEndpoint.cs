using MediatR;
using Teams.Application.Teams.Create;

namespace Teams.Api.Endpoints;

public static class CreateTeamEndpoint
{
    public static void MapCreateTeamEndpoint(this WebApplication app)
    {
        app.MapPost("/api/teams", async (
            CreateTeamCommand command,
            IMediator mediator,
            ILogger<Program> logger) =>
        {
            try
            {
                var result = await mediator.Send(command);

                logger.LogInformation(
                    "Team created successfully: Id={TeamId}, Name={TeamName}",
                    result.Id, result.Name);

                return Results.Created(
                    $"/api/teams/{result.Id}",
                    new
                    {
                        id = result.Id,
                        name = result.Name,
                        description = result.Description,
                        leaderId = result.LeaderId,
                        memberIds = result.MemberIds,
                        joinCode = result.JoinCode,
                        createdAt = result.CreatedAt
                    });
            }
            catch (FluentValidation.ValidationException ex)
            {
                logger.LogWarning(
                    "Team creation validation failed: {Message}", ex.Message);

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
            catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
            {
                logger.LogWarning(
                    "Team creation conflict: Name already exists — {Message}", ex.Message);

                return Results.Conflict(new
                {
                    error = "Conflict",
                    details = new[]
                    {
                        new { field = "Name", message = "A team with this name already exists." }
                    }
                });
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(
                    "Team creation failed: {Message}", ex.Message);

                return Results.BadRequest(new
                {
                    error = "Bad Request",
                    details = new[]
                    {
                        new { field = "", message = ex.Message }
                    }
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Team creation failed due to an unexpected error");

                var problem = new
                {
                    statusCode = StatusCodes.Status500InternalServerError,
                    title = "Team creation failed",
                    detail = "An unexpected error occurred while creating the team. Please try again later."
                };

                return Results.Problem(
                    problem.title,
                    null,
                    problem.statusCode,
                    problem.title,
                    problem.detail);
            }
        })
        .WithName("CreateTeam")
        .RequireAuthorization("operator_or_admin");
    }
}
