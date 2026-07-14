using MediatR;

namespace Missions.Application.Users.GetUsers;

public record GetUsersQuery(
    string? Search,
    string? Role,
    bool? Enabled,
    int Page,
    int PageSize) : IRequest<GetUsersResponse>;
