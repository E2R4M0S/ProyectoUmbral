using MediatR;
using Missions.Application.Missions.Catalog;
using Microsoft.AspNetCore.Mvc;

namespace Missions.Api.Endpoints;

public static class MissionCatalogEndpoints
{
    public static void MapMissionCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/").WithTags("Mission Catalog");

        // Register /active BEFORE /{id:guid} to prevent "active" from being parsed as a GUID
        group.MapGet("/active", async (
            [FromQuery] string? search,
            [FromQuery] string? difficulty,
            [FromQuery] int page,
            [FromQuery] int pageSize,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var query = new GetMissionsQuery(search, difficulty, "Active", page, pageSize);
            var result = await mediator.Send(query, ct);
            return Results.Ok(result);
        })
        .WithName("GetActiveMissions")
        .WithOpenApi()
        .RequireAuthorization("operator_or_admin");

        group.MapGet("/", async (
            [FromQuery] string? search,
            [FromQuery] string? difficulty,
            [FromQuery] string? status,
            [FromQuery] int page,
            [FromQuery] int pageSize,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var query = new GetMissionsQuery(search, difficulty, status, page, pageSize);
            var result = await mediator.Send(query, ct);
            return Results.Ok(result);
        })
        .WithName("GetMissions")
        .WithOpenApi()
        .RequireAuthorization("operator_or_admin");

        group.MapGet("/{id:guid}", async (
            Guid id,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var query = new GetMissionByIdQuery(id);
            var result = await mediator.Send(query, ct);
            if (result is null) return Results.NotFound();
            return Results.Ok(result);
        })
        .WithName("GetMissionById")
        .WithOpenApi()
        .RequireAuthorization("operator_or_admin");
    }
}