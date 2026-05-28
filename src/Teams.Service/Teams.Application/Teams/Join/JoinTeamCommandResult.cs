namespace Teams.Application.Teams.Join;

public record JoinTeamCommandResult(
    bool IsSuccess,
    Guid TeamId,
    string? ErrorMessage = null)
{
    public static JoinTeamCommandResult Success(Guid teamId) => new(true, teamId);
    public static JoinTeamCommandResult Failure(Guid teamId, string message) => new(false, teamId, message);
}
