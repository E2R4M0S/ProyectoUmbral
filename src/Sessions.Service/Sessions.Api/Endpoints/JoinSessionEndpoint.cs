using MediatR;
using Sessions.Application.Sessions.Join;
using Microsoft.AspNetCore.Mvc;

namespace Sessions.Api.Endpoints;

public static class JoinSessionEndpoint
{
    public static void MapJoinSessionEndpoint(this WebApplication app)
    {
        app.MapPost("/join", async (
            [FromBody] JoinSessionCommand command,
            IMediator mediator,
            ILogger<Program> logger) =>
        {
            try
            {
                var result = await mediator.Send(command);

                logger.LogInformation(
                    "User {UserId} joined session {SessionId}",
                    result.UserId, result.SessionId);

                return Results.Ok(result);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
            {
                logger.LogWarning(
                    "Join session failed: {Message}", ex.Message);

                return Results.NotFound(new
                {
                    error = "Not Found",
                    message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(
                    "Join session failed: {Message}", ex.Message);

                return Results.BadRequest(new
                {
                    error = "Cannot join session",
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Session join failed due to an unexpected error");

                var problem = new
                {
                    statusCode = StatusCodes.Status500InternalServerError,
                    title = "Session join failed",
                    detail = "An unexpected error occurred while joining the session. Please try again later."
                };

                return Results.Problem(
                    problem.title,
                    null,
                    problem.statusCode,
                    problem.title,
                    problem.detail);
            }
        })
        .WithName("JoinSession")
        .RequireAuthorization();
    }
}