using MediatR;
using Microsoft.AspNetCore.Http;
using Sessions.Application.Common;
using Sessions.Application.Common.Interfaces;

namespace Sessions.Application.Sessions.Consult;

public record GetMySessionsQuery : IRequest<List<MySessionDto>>;

public record MySessionDto(
    Guid Id,
    string Name,
    string Status,
    IReadOnlyList<string> MissionTitles,
    int MyScore,
    DateTime? StartedAt,
    DateTime? EndedAt,
    DateTime JoinedAt);

public class GetMySessionsQueryHandler : IRequestHandler<GetMySessionsQuery, List<MySessionDto>>
{
    private readonly ISessionRepository _repository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetMySessionsQueryHandler(ISessionRepository repository, IHttpContextAccessor httpContextAccessor)
    {
        _repository = repository;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<List<MySessionDto>> Handle(GetMySessionsQuery request, CancellationToken ct)
    {
        var userId = CurrentUserClaims.GetUserId(_httpContextAccessor.HttpContext?.User);
        if (userId is null)
        {
            return new List<MySessionDto>();
        }

        var sessions = await _repository.GetSessionsForParticipantAsync(userId.Value, ct);

        return sessions.Select(s =>
        {
            var me = s.Participants.First(p => p.UserId == userId.Value);
            return new MySessionDto(
                s.Id,
                s.Name,
                s.Status.ToString(),
                s.Stages.Select(st => st.MissionTitle).Distinct().ToList(),
                me.Score,
                s.StartedAt,
                s.EndedAt,
                me.JoinedAt);
        }).ToList();
    }
}
