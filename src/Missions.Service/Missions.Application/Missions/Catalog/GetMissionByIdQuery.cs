using MediatR;

namespace Missions.Application.Missions.Catalog;

public record GetMissionByIdQuery(Guid Id) : IRequest<MissionDetailDto?>;