using MediatR;

namespace Teams.Application.Teams.Join;

public record JoinTeamCommand(
    Guid TeamId,
    string JoinCode) : IRequest<JoinTeamCommandResult>;
