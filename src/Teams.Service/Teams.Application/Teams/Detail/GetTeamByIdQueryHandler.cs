using MediatR;
using Teams.Application.Common.Interfaces;

namespace Teams.Application.Teams.Detail;

public class GetTeamByIdQueryHandler : IRequestHandler<GetTeamByIdQuery, TeamDetailDto?>
{
    private readonly ITeamRepository _repository;

    public GetTeamByIdQueryHandler(ITeamRepository repository)
    {
        _repository = repository;
    }

    public async Task<TeamDetailDto?> Handle(GetTeamByIdQuery request, CancellationToken ct)
    {
        var team = await _repository.GetByIdWithMembersAsync(request.Id, ct);

        if (team is null) return null;

        return new TeamDetailDto(
            team.Id,
            team.Name,
            team.Description,
            team.LeaderId,
            team.JoinCode,
            team.Members
                .Select(m => new MemberDto(m.Id, m.UserId))
                .ToList());
    }
}