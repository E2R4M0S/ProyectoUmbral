namespace Teams.Application.Teams.Users.GetUsers;

public record UserListItem(
    string Id,
    string Name,
    string Email,
    string[] Roles,
    bool Enabled,
    DateTime CreatedAt);