using MediatR;

namespace Sessions.Application.Sessions.Teams.CreateTeam;

public record CreateSessionTeamCommand(Guid SessionId, string Name) : IRequest<CreateSessionTeamResult>;
