using MediatR;
using Teams.Application.Common.Interfaces;

namespace Teams.Application.Teams.Detail;

public class GetTeamByIdQueryHandler : IRequestHandler<GetTeamByIdQuery, TeamDetailDto?>
{
    private readonly ITeamRepository _repository;
    private readonly IKeycloakAdminService _keycloak;

    public GetTeamByIdQueryHandler(ITeamRepository repository, IKeycloakAdminService keycloak)
    {
        _repository = repository;
        _keycloak = keycloak;
    }

    public async Task<TeamDetailDto?> Handle(GetTeamByIdQuery request, CancellationToken ct)
    {
        var team = await _repository.GetByIdWithMembersAsync(request.Id, ct);

        if (team is null) return null;

        var leaderName = team.LeaderId[..8] + "...";
        try
        {
            var leader = await _keycloak.GetUserByIdAsync(team.LeaderId, ct);
            if (leader is not null)
                leaderName = leader.FirstName ?? leader.Email ?? leaderName;
        }
        catch { }

        var memberDtos = new List<MemberDto>();
        foreach (var m in team.Members)
        {
            var name = m.UserId[..8] + "...";
            try
            {
                var user = await _keycloak.GetUserByIdAsync(m.UserId, ct);
                if (user is not null)
                    name = user.FirstName ?? user.Email ?? name;
            }
            catch { /* fallback to truncated ID */ }
            memberDtos.Add(new MemberDto(m.Id, m.UserId, name));
        }

        return new TeamDetailDto(
            team.Id,
            team.Name,
            team.Description,
            team.LeaderId,
            leaderName,
            team.JoinCode,
            memberDtos);
    }
}