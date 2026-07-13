using MediatR;
using Teams.Application.Teams.Mine;

namespace Teams.Api.Endpoints;

public static class GetMyTeamsEndpoint
{
    public static void MapGetMyTeamsEndpoint(this WebApplication app)
    {
        app.MapGet("/api/teams/mine", async (
            HttpContext context,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = context.User.FindFirst("sub")?.Value
                ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            var query = new GetMyTeamsQuery(userId);
            var result = await mediator.Send(query, ct);
            return Results.Ok(result);
        })
        .WithName("GetMyTeams")
        .RequireAuthorization("authenticated");
    }
}
