using MediatR;

namespace Sessions.Application.Sessions.Consult;

public record GetSessionsQuery(
    string? Search,
    string? Status,
    Guid? MissionId,
    int Page,
    int PageSize
) : IRequest<GetSessionsResult>;