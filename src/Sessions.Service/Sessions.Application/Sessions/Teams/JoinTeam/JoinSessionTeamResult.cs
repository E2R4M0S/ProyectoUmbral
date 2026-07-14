namespace Sessions.Application.Sessions.Teams.JoinTeam;

public record JoinSessionTeamResult(Guid TeamId, string TeamName, Guid UserId, string UserAlias);
