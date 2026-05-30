using MediatR;
using Sessions.Application.Sessions.Consult;
using Microsoft.AspNetCore.Mvc;

namespace Sessions.Api.Endpoints;

public static class GetSessionsEndpoint
{
    public static void MapGetSessionsEndpoint(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/").WithTags("Sessions");

        group.MapGet("/", async (
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] Guid? missionId,
            [FromQuery] int page,
            [FromQuery] int pageSize,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var query = new GetSessionsQuery(search, status, missionId, page, pageSize);
            var result = await mediator.Send(query, ct);
            return Results.Ok(result);
        })
        .WithName("GetSessions")
        .WithOpenApi()
        .RequireAuthorization("operator_or_admin");
    }
}