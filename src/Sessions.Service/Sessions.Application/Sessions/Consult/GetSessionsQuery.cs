using MediatR;

namespace Sessions.Application.Sessions.Consult;

public record GetSessionsQuery(
    string? Search,
    string? Status,
    Guid? MissionId,
    int Page,
    int PageSize,
    // RB-10: null means "no ownership filter" (admin, read-only supervision).
    // Set means "only sessions owned by this operator".
    Guid? OperatorId = null
) : IRequest<GetSessionsResult>;