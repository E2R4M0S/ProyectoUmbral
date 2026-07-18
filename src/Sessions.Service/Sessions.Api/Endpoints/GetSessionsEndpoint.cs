using MediatR;
using Sessions.Application.Common;
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
            HttpContext httpContext,
            IMediator mediator,
            CancellationToken ct) =>
        {
            // RB-10: an operator only ever lists the sessions they own; an admin
            // supervises everything in read-only mode, so no ownership filter applies.
            var isAdmin = httpContext.User.IsInRole("admin");
            var operatorId = isAdmin ? null : CurrentUserClaims.GetUserId(httpContext.User);

            var query = new GetSessionsQuery(search, status, missionId, page, pageSize, operatorId);
            var result = await mediator.Send(query, ct);
            return Results.Ok(result);
        })
        .WithName("GetSessions")
        .WithOpenApi()
        .RequireAuthorization("operator_or_admin");
    }
}