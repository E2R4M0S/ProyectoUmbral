using MediatR;
using Teams.Application.Common.Interfaces;

namespace Teams.Application.Teams.Mine;

public class GetMyTeamsQueryHandler : IRequestHandler<GetMyTeamsQuery, IEnumerable<MyTeamDto>>
{
    private readonly ITeamRepository _repository;

    public GetMyTeamsQueryHandler(ITeamRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<MyTeamDto>> Handle(GetMyTeamsQuery request, CancellationToken ct)
    {
        var teams = await _repository.GetByMemberIdAsync(request.UserId, ct);
        return teams.Select(t => new MyTeamDto(
            t.Id,
            t.Name,
            t.Members.Select(m => m.UserId).ToList()));
    }
}
