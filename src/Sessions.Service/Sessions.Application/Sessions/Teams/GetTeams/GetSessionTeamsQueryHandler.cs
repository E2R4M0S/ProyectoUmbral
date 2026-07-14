using MediatR;
using Sessions.Application.Common.Interfaces;

namespace Sessions.Application.Sessions.Teams.GetTeams;

public class GetSessionTeamsQueryHandler : IRequestHandler<GetSessionTeamsQuery, List<SessionTeamDto>>
{
    private readonly ISessionRepository _repository;

    public GetSessionTeamsQueryHandler(ISessionRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<SessionTeamDto>> Handle(GetSessionTeamsQuery request, CancellationToken ct)
    {
        var teams = await _repository.GetTeamsBySessionIdAsync(request.SessionId, ct);

        return teams.Select(t => new SessionTeamDto(
            t.Id,
            t.Name,
            t.Members.Count,
            t.MaxMembers,
            t.Members
                .Select(m => new SessionTeamMemberDto(m.UserId, m.UserAlias, m.JoinedAt))
                .ToList()
        )).ToList();
    }
}
