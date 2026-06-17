namespace Sessions.Domain.Entities;

public class SessionStage
{
    public Guid MissionId { get; private set; }
    public string MissionTitle { get; private set; } = null!;
    public string MissionType { get; private set; } = null!;
    public int Order { get; private set; }

    private SessionStage() { }

    public static SessionStage Create(Guid missionId, string missionTitle, string missionType, int order)
    {
        if (missionId == Guid.Empty)
            throw new InvalidOperationException("SessionStage MissionId cannot be empty");
        if (string.IsNullOrWhiteSpace(missionTitle))
            throw new InvalidOperationException("SessionStage MissionTitle is required");
        if (string.IsNullOrWhiteSpace(missionType))
            throw new InvalidOperationException("SessionStage MissionType is required");
        if (order < 1)
            throw new InvalidOperationException("SessionStage Order must be a positive integer");

        return new SessionStage
        {
            MissionId = missionId,
            MissionTitle = missionTitle.Trim(),
            MissionType = missionType,
            Order = order
        };
    }
}
