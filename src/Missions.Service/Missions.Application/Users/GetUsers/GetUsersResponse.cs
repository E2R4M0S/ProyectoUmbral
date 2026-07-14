namespace Missions.Application.Users.GetUsers;

public record GetUsersResponse(UserListItem[] Items, int TotalCount, int Page, int PageSize);
