namespace Sessions.Application.Sessions.Teams.GetTeams;

public record SessionTeamDto(
    Guid Id,
    string Name,
    int MemberCount,
    int MaxMembers,
    List<SessionTeamMemberDto> Members);

public record SessionTeamMemberDto(Guid UserId, string UserAlias, DateTime JoinedAt);
