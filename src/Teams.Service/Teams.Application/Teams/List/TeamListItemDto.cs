namespace Teams.Application.Teams.List;

public record TeamListItemDto(
    Guid Id,
    string Name,
    string Description,
    string LeaderId,
    int MemberCount,
    string? JoinCode
);
