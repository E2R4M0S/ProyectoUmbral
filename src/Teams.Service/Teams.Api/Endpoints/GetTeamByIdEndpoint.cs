using MediatR;
using Teams.Application.Teams.Detail;

namespace Teams.Api.Endpoints;

public static class GetTeamByIdEndpoint
{
    public static void MapGetTeamByIdEndpoint(this WebApplication app)
    {
        app.MapGet("/api/teams/{id:guid}", async (
            Guid id,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var query = new GetTeamByIdQuery(id);
            var result = await mediator.Send(query, ct);

            if (result is null)
                return Results.NotFound();

            return Results.Ok(result);
        })
        .WithName("GetTeamById")
        .WithOpenApi()
        .RequireAuthorization("operator_or_admin");
    }
}