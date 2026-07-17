using MediatR;
using Sessions.Application.Common.Interfaces;

namespace Sessions.Application.Sessions.Consult;

public record GetAuditLogQuery(Guid SessionId) : IRequest<List<AuditEventDto>?>;

public record AuditEventDto(
    Guid Id,
    string EventType,
    string Description,
    Guid? TeamId,
    Guid? UserId,
    int? ScoreDelta,
    DateTime OccurredAt);

public class GetAuditLogQueryHandler : IRequestHandler<GetAuditLogQuery, List<AuditEventDto>?>
{
    private readonly ISessionRepository _repository;

    public GetAuditLogQueryHandler(ISessionRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<AuditEventDto>?> Handle(GetAuditLogQuery query, CancellationToken ct)
    {
        var session = await _repository.GetByIdAsync(query.SessionId, ct);
        if (session is null)
        {
            return null;
        }

        var events = await _repository.GetAuditTrailAsync(query.SessionId, ct);

        return events
            .Select(e => new AuditEventDto(e.Id, e.EventType, e.Description, e.TeamId, e.UserId, e.ScoreDelta, e.OccurredAt))
            .ToList();
    }
}
