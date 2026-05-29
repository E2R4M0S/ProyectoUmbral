namespace Teams.Application.Teams.Create;

public record CreateTeamCommandResult(
    Guid Id,
    string Name,
    string Description,
    string LeaderId,
    List<string> MemberIds,
    string? JoinCode,
    DateTime CreatedAt);
