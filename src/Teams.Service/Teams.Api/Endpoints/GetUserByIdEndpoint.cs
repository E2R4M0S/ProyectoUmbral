using MediatR;
using Teams.Application.Teams.Users.GetUserById;

namespace Teams.Api.Endpoints;

public static class GetUserByIdEndpoint
{
    public static void MapGetUserByIdEndpoint(this WebApplication app)
    {
        app.MapGet("/users/{id}", async (
            string id,
            IMediator mediator,
            ILogger<Program> logger) =>
        {
            try
            {
                var query = new GetUserByIdQuery(id);
                var result = await mediator.Send(query);

                if (result is null)
                {
                    logger.LogWarning("User not found: UserId={UserId}", id);
                    return Results.NotFound(new { error = "User not found", userId = id });
                }

                logger.LogInformation("User retrieved: UserId={UserId}, Email={Email}", id, result.Email);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to retrieve user: UserId={UserId}", id);
                return Results.Problem(
                    "User retrieval failed",
                    null,
                    StatusCodes.Status500InternalServerError,
                    "User retrieval failed",
                    "An unexpected error occurred while retrieving the user profile.");
            }
        })
        .WithName("GetUserById")
        .RequireAuthorization("admin");
    }
}