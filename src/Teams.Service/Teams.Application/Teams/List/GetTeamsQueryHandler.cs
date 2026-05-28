using MediatR;
using Teams.Application.Common.Interfaces;
using Teams.Application.Teams.List;

namespace Teams.Application.Teams.List;

public class GetTeamsQueryHandler : IRequestHandler<GetTeamsQuery, GetTeamsResult>
{
    private readonly ITeamRepository _repository;

    public GetTeamsQueryHandler(ITeamRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetTeamsResult> Handle(GetTeamsQuery request, CancellationToken ct)
    {
        if (request.Page < 1) request = request with { Page = 1 };
        if (request.PageSize < 1) request = request with { PageSize = 10 };

        var (teams, totalCount) = await _repository.GetTeamsAsync(
            request.Search,
            request.Page,
            request.PageSize,
            ct);

        var items = teams.Select(t => new TeamListItemDto(
            t.Id,
            t.Name,
            t.Description,
            t.LeaderId,
            t.Members.Count,
            t.JoinCode
        )).ToList();

        return new GetTeamsResult(items, totalCount, request.Page, request.PageSize);
    }
}
