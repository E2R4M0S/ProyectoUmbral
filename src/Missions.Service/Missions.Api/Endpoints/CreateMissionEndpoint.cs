using MediatR;
using Missions.Application.Missions.Create;
using Microsoft.AspNetCore.Mvc;

namespace Missions.Api.Endpoints;

public static class CreateMissionEndpoint
{
    public static void MapCreateMissionEndpoint(this WebApplication app)
    {
        app.MapPost("/missions", async (
            [FromBody] CreateMissionCommand command,
            IMediator mediator,
            ILogger<Program> logger) =>
        {
            try
            {
                var result = await mediator.Send(command);

                logger.LogInformation(
                    "Mission created successfully: Id={MissionId}, Title={Title}",
                    result.Id, result.Title);

                return Results.Created(
                    $"/missions/{result.Id}",
                    new
                    {
                        id = result.Id,
                        title = result.Title,
                        description = result.Description,
                        difficulty = result.Difficulty,
                        timeMinutes = result.TimeMinutes,
                        type = result.Type,
                        status = result.Status,
                        createdAt = result.CreatedAt
                    });
            }
            catch (FluentValidation.ValidationException ex)
            {
                logger.LogWarning(
                    "Mission creation validation failed: {Message}", ex.Message);

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
                    "Mission creation conflict: Title already exists — {Message}", ex.Message);

                return Results.Conflict(new
                {
                    error = "Conflict",
                    details = new[]
                    {
                        new { field = "Title", message = "A mission with this title already exists." }
                    }
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Mission creation failed due to an unexpected error");

                var problem = new
                {
                    statusCode = StatusCodes.Status500InternalServerError,
                    title = "Mission creation failed",
                    detail = "An unexpected error occurred while creating the mission. Please try again later."
                };

                return Results.Problem(
                    problem.title,
                    null,
                    problem.statusCode,
                    problem.title,
                    problem.detail);
            }
        })
        .WithName("CreateMission")
        .RequireAuthorization("admin");
    }
}