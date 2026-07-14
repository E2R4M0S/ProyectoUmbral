using MediatR;

namespace Sessions.Application.Sessions.Teams.GetTeams;

public record GetSessionTeamsQuery(Guid SessionId) : IRequest<List<SessionTeamDto>>;
