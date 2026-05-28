using MediatR;

namespace Teams.Application.Teams.Detail;

public record GetTeamByIdQuery(Guid Id) : IRequest<TeamDetailDto?>;