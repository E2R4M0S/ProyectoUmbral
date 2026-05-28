using MediatR;
using Teams.Application.Teams.List;

namespace Teams.Api.Endpoints;

public static class GetTeamsEndpoint
{
    public static void MapGetTeamsEndpoint(this WebApplication app)
    {
        app.MapGet("/api/teams", async (
            string? search,
            int page,
            int pageSize,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var query = new GetTeamsQuery(search, page, pageSize);
            var result = await mediator.Send(query, ct);
            return Results.Ok(result);
        })
        .WithName("GetTeams")
        .WithOpenApi()
        .RequireAuthorization("operator_or_admin");
    }
}
