namespace Missions.Application.Missions.Catalog;

public record GetMissionsResult(
    IReadOnlyList<MissionListItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);