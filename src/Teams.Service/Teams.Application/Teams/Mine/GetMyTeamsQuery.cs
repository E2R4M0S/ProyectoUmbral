using MediatR;

namespace Teams.Application.Teams.Mine;

public record GetMyTeamsQuery(string UserId) : IRequest<IEnumerable<MyTeamDto>>;

public record MyTeamDto(Guid Id, string Name, IReadOnlyList<string> MemberIds);
