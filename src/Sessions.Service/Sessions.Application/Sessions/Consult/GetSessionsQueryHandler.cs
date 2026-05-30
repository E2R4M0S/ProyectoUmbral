using MediatR;
using Sessions.Application.Common.Interfaces;

namespace Sessions.Application.Sessions.Consult;

public class GetSessionsQueryHandler : IRequestHandler<GetSessionsQuery, GetSessionsResult>
{
    private readonly ISessionRepository _repository;

    public GetSessionsQueryHandler(ISessionRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetSessionsResult> Handle(GetSessionsQuery request, CancellationToken ct)
    {
        if (request.Page < 1) request = request with { Page = 1 };
        if (request.PageSize < 1) request = request with { PageSize = 10 };

        var (sessions, totalCount) = await _repository.GetSessionsAsync(
            request.Search,
            request.Status,
            request.MissionId,
            request.Page,
            request.PageSize,
            ct);

        var items = sessions.Select(s => new SessionListItemDto(
            s.Id,
            s.Name,
            s.MissionId,
            s.MissionTitle,
            s.Pin,
            s.Participants.Count,
            s.Status.ToString(),
            s.StartedAt,
            s.EndedAt,
            s.CreatedAt
        )).ToList();

        return new GetSessionsResult(items, totalCount, request.Page, request.PageSize);
    }
}