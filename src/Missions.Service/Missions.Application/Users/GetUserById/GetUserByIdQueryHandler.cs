using MediatR;
using Missions.Application.Common.Interfaces;

namespace Missions.Application.Users.GetUserById;

public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDetailResponse?>
{
    private readonly IKeycloakAdminService _keycloakService;

    public GetUserByIdQueryHandler(IKeycloakAdminService keycloakService)
    {
        _keycloakService = keycloakService;
    }

    public async Task<UserDetailResponse?> Handle(GetUserByIdQuery query, CancellationToken ct)
    {
        var user = await _keycloakService.GetUserByIdAsync(query.UserId, ct);
        if (user is null)
            return null;

        var roles = await _keycloakService.GetUserRealmRolesAsync(query.UserId, ct);
        var name = user.FirstName ?? user.Email.Split('@')[0];

        return new UserDetailResponse(
            user.Id,
            name,
            user.Email,
            roles.ToArray(),
            user.Enabled,
            DateTimeOffset.FromUnixTimeMilliseconds(user.CreatedTimestamp).DateTime,
            user.Attributes,
            user.EmailVerified);
    }
}
