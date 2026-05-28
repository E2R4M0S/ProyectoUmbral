using MediatR;
using Missions.Application.Common.Interfaces;

namespace Missions.Application.Missions.Catalog;

public class GetMissionByIdQueryHandler : IRequestHandler<GetMissionByIdQuery, MissionDetailDto?>
{
    private readonly IMissionRepository _repository;

    public GetMissionByIdQueryHandler(IMissionRepository repository)
    {
        _repository = repository;
    }

    public async Task<MissionDetailDto?> Handle(GetMissionByIdQuery request, CancellationToken ct)
    {
        var mission = await _repository.GetByIdAsync(request.Id, ct);

        if (mission is null) return null;

        return new MissionDetailDto(
            mission.Id,
            mission.Title,
            mission.Description,
            mission.Difficulty.ToString(),
            mission.TimeMinutes,
            mission.Type.ToString(),
            mission.Status.ToString()
        );
    }
}