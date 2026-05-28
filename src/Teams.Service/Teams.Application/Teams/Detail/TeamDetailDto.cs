namespace Teams.Application.Teams.Detail;

public record TeamDetailDto(
    Guid Id,
    string Name,
    string Description,
    string LeaderId,
    string? JoinCode,
    IReadOnlyList<MemberDto> Members);