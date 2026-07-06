using MediatR;
using Missions.Application.Common.Interfaces;
using Missions.Application.Missions.Clues;
using Missions.Application.Missions.Stages;

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
            mission.Status.ToString(),
            mission.Stages
                .OrderBy(s => s.Order)
                .Select(s => new StageDto(
                    s.Id,
                    s.Name,
                    s.Description,
                    s.Order,
                    s.QrToken,
                    s.Latitude,
                    s.Longitude,
                    s.Clues
                        .Select(c => new ClueDto(c.Id, c.Content, c.Penalty, c.ReleaseType.ToString()))
                        .ToList()))
                .ToList()
        );
    }
}
