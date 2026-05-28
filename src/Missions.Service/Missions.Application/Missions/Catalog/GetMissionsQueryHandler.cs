using MediatR;
using Missions.Application.Common.Interfaces;
using DomainEnums = Missions.Domain.Enums;

namespace Missions.Application.Missions.Catalog;

public class GetMissionsQueryHandler : IRequestHandler<GetMissionsQuery, GetMissionsResult>
{
    private readonly IMissionRepository _repository;

    public GetMissionsQueryHandler(IMissionRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetMissionsResult> Handle(GetMissionsQuery request, CancellationToken ct)
    {
        if (request.Page < 1) request = request with { Page = 1 };
        if (request.PageSize < 1) request = request with { PageSize = 10 };

        var (missions, totalCount) = await _repository.GetMissionsAsync(
            request.Search,
            request.Difficulty,
            request.Status,
            request.Page,
            request.PageSize,
            ct);

        var items = missions.Select(m => new MissionListItemDto(
            m.Id,
            m.Title,
            m.Difficulty.ToString(),
            m.Type.ToString(),
            m.Status.ToString()
        )).ToList();

        return new GetMissionsResult(items, totalCount, request.Page, request.PageSize);
    }
}