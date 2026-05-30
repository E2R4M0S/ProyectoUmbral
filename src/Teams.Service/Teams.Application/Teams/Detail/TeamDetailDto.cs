namespace Teams.Application.Teams.Detail;

public record TeamDetailDto(
    Guid Id,
    string Name,
    string Description,
    string LeaderId,
    string LeaderName,
    string? JoinCode,
    DateTime CreatedAt,
    IReadOnlyList<MemberDto> Members);