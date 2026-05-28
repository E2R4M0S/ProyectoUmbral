namespace Sessions.Application.Sessions.Consult;

public record GetSessionsResult(
    IReadOnlyList<SessionListItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);