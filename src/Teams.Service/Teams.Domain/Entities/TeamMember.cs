namespace Teams.Domain.Entities;

public class TeamMember
{
    public Guid Id { get; private set; }
    public Guid TeamId { get; private set; }
    public string UserId { get; private set; } = null!;

    private TeamMember() { }

    internal TeamMember(Guid teamId, string userId)
    {
        Id = Guid.NewGuid();
        TeamId = teamId;
        UserId = userId;
    }

    public static TeamMember Create(Guid teamId, string userId) => new(teamId, userId);
}
