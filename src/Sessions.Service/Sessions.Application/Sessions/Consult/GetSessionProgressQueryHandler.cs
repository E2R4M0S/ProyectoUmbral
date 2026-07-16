using MediatR;
using Sessions.Application.Common.Interfaces;
using Sessions.Application.Sessions.Consult;

namespace Sessions.Application.Sessions.Consult;

public record GetSessionProgressQuery(Guid SessionId) : IRequest<SessionProgressDto?>;

public class GetSessionProgressQueryHandler : IRequestHandler<GetSessionProgressQuery, SessionProgressDto?>
{
    private readonly ISessionRepository _repository;

    public GetSessionProgressQueryHandler(ISessionRepository repository)
    {
        _repository = repository;
    }

    public async Task<SessionProgressDto?> Handle(GetSessionProgressQuery query, CancellationToken ct)
    {
        var session = await _repository.GetByIdWithStagesAsync(query.SessionId, ct);
        if (session is null)
        {
            return null;
        }

        var elapsed = session.StartedAt.HasValue
            ? (int)((session.EndedAt ?? DateTime.UtcNow) - session.StartedAt.Value).TotalSeconds
            : 0;

        // Each stage within a mission carries that mission's full TimeMinutes (not a per-stage
        // share), so sum once per distinct mission to get the session's true total duration.
        var totalDuration = session.Stages
            .GroupBy(s => s.MissionId)
            .Sum(g => g.First().TimeMinutes == -1 ? 10 : Math.Max(0, g.First().TimeMinutes) * 60);

        var missionElapsed = session.CurrentMissionStartedAt.HasValue
            ? (int)((session.EndedAt ?? DateTime.UtcNow) - session.CurrentMissionStartedAt.Value).TotalSeconds
            : 0;

        return new SessionProgressDto(
            session.Id,
            session.Name,
            session.Status.ToString(),
            elapsed,
            totalDuration,
            missionElapsed,
            session.Participants.Select(p => new ParticipantProgressDto(p.UserId, p.UserAlias, p.JoinedAt)).ToList());
    }
}
