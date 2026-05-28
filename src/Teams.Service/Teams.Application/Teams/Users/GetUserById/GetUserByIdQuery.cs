using MediatR;
using Teams.Application.Teams.Users.GetUserById;

namespace Teams.Application.Teams.Users.GetUserById;

public record GetUserByIdQuery(string UserId) : IRequest<UserDetailResponse?>;