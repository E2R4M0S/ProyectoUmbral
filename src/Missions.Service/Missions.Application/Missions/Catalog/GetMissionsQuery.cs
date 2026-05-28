using MediatR;

namespace Missions.Application.Missions.Catalog;

public record GetMissionsQuery(
    string? Search,
    string? Difficulty,
    string? Status,
    int Page,
    int PageSize
) : IRequest<GetMissionsResult>;