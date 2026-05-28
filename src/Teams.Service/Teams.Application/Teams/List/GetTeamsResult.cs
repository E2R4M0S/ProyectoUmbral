namespace Teams.Application.Teams.List;

public record GetTeamsResult(
    IReadOnlyList<TeamListItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);
