using MediatR;

namespace Sessions.Application.Sessions.Teams.JoinTeam;

public record JoinSessionTeamCommand(Guid SessionId, Guid TeamId) : IRequest<JoinSessionTeamResult>;
