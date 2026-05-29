using MediatR;

namespace Teams.Application.Teams.Join;

public record JoinTeamCommand(
    string JoinCode) : IRequest<JoinTeamCommandResult>;
