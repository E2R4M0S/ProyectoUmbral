using MediatR;

namespace Missions.Application.Users.GetUserById;

public record GetUserByIdQuery(string UserId) : IRequest<UserDetailResponse?>;
