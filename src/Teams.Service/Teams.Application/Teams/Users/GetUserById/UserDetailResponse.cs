using System.Collections.Immutable;

namespace Teams.Application.Teams.Users.GetUserById;

public record UserDetailResponse(
    string Id,
    string Name,
    string Email,
    string[] Roles,
    bool Enabled,
    DateTime CreatedAt,
    ImmutableDictionary<string, string[]> Attributes,
    bool EmailVerified);