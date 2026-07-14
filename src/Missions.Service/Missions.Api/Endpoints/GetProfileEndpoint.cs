using MediatR;
using Missions.Application.Participants.Profile;

namespace Missions.Api.Endpoints;

public static class GetProfileEndpoint
{
    public static void MapGetProfileEndpoint(this WebApplication app)
    {
        app.MapGet("/api/profile", async (
            HttpContext context,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            var keycloakUserId = context.User.FindFirst("sub")?.Value
                ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(keycloakUserId))
            {
                logger.LogWarning("GetProfile failed: missing sub claim in JWT");
                return Results.Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Unauthorized",
                    detail: "Missing or invalid JWT token.");
            }

            try
            {
                var query = new GetProfileQuery(keycloakUserId);
                var response = await mediator.Send(query, ct);

                if (response is null)
                    return Results.NotFound(new { error = "Not Found", detail = "Participant not found" });

                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GetProfile failed for KeycloakUserId={KeycloakUserId}", keycloakUserId);
                return Results.Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Profile retrieval failed",
                    detail: "An unexpected error occurred while retrieving the profile.");
            }
        })
        .WithName("GetProfile")
        .RequireAuthorization("participant");
    }
}
