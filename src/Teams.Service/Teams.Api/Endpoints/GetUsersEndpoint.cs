using System.Security.Claims;
using MediatR;
using Teams.Application.Teams.Users.GetUsers;

namespace Teams.Api.Endpoints;

public static class GetUsersEndpoint
{
    public static void MapGetUsersEndpoint(this WebApplication app)
    {
        app.MapGet("/users", async (
            HttpContext httpContext,
            string? search,
            string? role,
            bool? enabled,
            int? page,
            int? pageSize,
            IMediator mediator,
            ILogger<Program> logger) =>
        {
            try
            {
                var query = new GetUsersQuery(
                    Search: search,
                    Role: role,
                    Enabled: enabled,
                    Page: page ?? 1,
                    PageSize: pageSize ?? 20);

                var result = await mediator.Send(query);

                // Operators should NOT see admin users
                var isAdmin = httpContext.User.IsInRole("admin");
                if (!isAdmin)
                {
                    result = result with
                    {
                        Items = result.Items.Where(i => !i.Roles.Contains("admin") && !i.Roles.Contains("operator")).ToArray(),
                        TotalCount = result.Items.Count(i => !i.Roles.Contains("admin") && !i.Roles.Contains("operator"))
                    };
                }

                logger.LogInformation(
                    "Users listed: TotalCount={TotalCount}, Page={Page}, PageSize={PageSize}",
                    result.TotalCount, result.Page, result.PageSize);

                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to list users");
                return Results.Problem(
                    "User listing failed",
                    null,
                    StatusCodes.Status500InternalServerError,
                    "User listing failed",
                    "An unexpected error occurred while retrieving the user list.");
            }
        })
        .WithName("GetUsers")
        .RequireAuthorization("operator_or_admin");
    }
}