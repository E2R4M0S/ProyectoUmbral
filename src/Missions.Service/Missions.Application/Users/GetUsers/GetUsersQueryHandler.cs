using MediatR;
using Missions.Application.Common.Interfaces;

namespace Missions.Application.Users.GetUsers;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, GetUsersResponse>
{
    private readonly IKeycloakAdminService _keycloakService;

    public GetUsersQueryHandler(IKeycloakAdminService keycloakService)
    {
        _keycloakService = keycloakService;
    }

    public async Task<GetUsersResponse> Handle(GetUsersQuery query, CancellationToken ct)
    {
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var first = (query.Page - 1) * pageSize;

        IReadOnlyList<UserRepresentation> users;

        if (!string.IsNullOrWhiteSpace(query.Role))
        {
            users = await _keycloakService.GetUsersByRoleAsync(query.Role, first, pageSize, ct);
        }
        else
        {
            users = await _keycloakService.GetUsersAsync(first, pageSize, query.Search, query.Enabled, ct);
        }

        if (users.Count == 0)
        {
            return new GetUsersResponse([], 0, query.Page, pageSize);
        }

        var roleTasks = users.Select(u => _keycloakService.GetUserRealmRolesAsync(u.Id, ct));
        var roleArrays = await Task.WhenAll(roleTasks);

        var items = users.Select((u, i) =>
        {
            var roles = roleArrays[i];
            var name = u.FirstName ?? u.Email.Split('@')[0];
            return new UserListItem(
                u.Id,
                name,
                u.Email,
                roles.ToArray(),
                u.Enabled,
                DateTimeOffset.FromUnixTimeMilliseconds(u.CreatedTimestamp).DateTime);
        }).ToArray();

        if (!string.IsNullOrWhiteSpace(query.Role))
        {
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var searchLower = query.Search.ToLowerInvariant();
                items = items.Where(i =>
                    i.Name.ToLowerInvariant().Contains(searchLower) ||
                    i.Email.ToLowerInvariant().Contains(searchLower)).ToArray();
            }

            if (query.Enabled.HasValue)
            {
                items = items.Where(i => i.Enabled == query.Enabled.Value).ToArray();
            }
        }

        return new GetUsersResponse(items, items.Length, query.Page, pageSize);
    }
}
