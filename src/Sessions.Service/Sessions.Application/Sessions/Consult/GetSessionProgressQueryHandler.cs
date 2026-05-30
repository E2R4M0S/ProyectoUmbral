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
        var session = await _repository.GetByIdAsync(query.SessionId, ct);
        if (session is null)
        {
            return null;
        }

        var elapsed = session.StartedAt.HasValue
            ? (int)((session.EndedAt ?? DateTime.UtcNow) - session.StartedAt.Value).TotalSeconds
            : 0;

        return new SessionProgressDto(
            session.Id,
            session.Name,
            session.Status.ToString(),
            elapsed,
            session.Participants.Select(p => new ParticipantProgressDto(p.UserId, p.UserAlias, p.JoinedAt)).ToList());
    }
}
