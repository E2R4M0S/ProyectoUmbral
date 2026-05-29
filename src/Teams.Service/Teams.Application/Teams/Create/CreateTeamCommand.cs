using MediatR;
using Teams.Application.Teams.Create;

namespace Teams.Application.Teams.Create;

public record CreateTeamCommand(
    string Name,
    string Description,
    string LeaderId,
    List<string>? MemberIds = null) : IRequest<CreateTeamCommandResult>;
