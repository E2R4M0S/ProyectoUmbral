using MediatR;

namespace Teams.Application.Teams.List;

public record GetTeamsQuery(
    string? Search,
    int Page,
    int PageSize
) : IRequest<GetTeamsResult>;
