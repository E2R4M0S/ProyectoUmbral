using MediatR;
using Teams.Application.Teams.Users.GetUsers;

namespace Teams.Application.Teams.Users.GetUsers;

public record GetUsersQuery(
    string? Search,
    string? Role,
    bool? Enabled,
    int Page,
    int PageSize) : IRequest<GetUsersResponse>;