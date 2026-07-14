namespace Sessions.Application.Sessions.Teams.CreateTeam;

public record CreateSessionTeamResult(Guid TeamId, string Name, int MaxMembers, DateTime CreatedAt);
