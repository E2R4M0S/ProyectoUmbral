namespace Sessions.Domain.Entities;

public class SessionStage
{
    public Guid MissionId { get; private set; }
    public Guid MissionStageId { get; private set; }
    public string MissionTitle { get; private set; } = null!;
    public string MissionType { get; private set; } = null!;
    public int Order { get; private set; }
    public string QrToken { get; private set; } = null!;
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }

    private SessionStage() { }

    public static SessionStage Create(
        Guid missionId,
        Guid missionStageId,
        string missionTitle,
        string missionType,
        int order,
        string qrToken,
        double? latitude = null,
        double? longitude = null)
    {
        if (missionId == Guid.Empty)
            throw new InvalidOperationException("SessionStage MissionId cannot be empty");
        if (missionStageId == Guid.Empty)
            throw new InvalidOperationException("SessionStage MissionStageId cannot be empty");
        if (string.IsNullOrWhiteSpace(missionTitle))
            throw new InvalidOperationException("SessionStage MissionTitle is required");
        if (string.IsNullOrWhiteSpace(missionType))
            throw new InvalidOperationException("SessionStage MissionType is required");
        if (order < 1)
            throw new InvalidOperationException("SessionStage Order must be a positive integer");
        if (string.IsNullOrWhiteSpace(qrToken))
            throw new InvalidOperationException("SessionStage QrToken is required");

        return new SessionStage
        {
            MissionId = missionId,
            MissionStageId = missionStageId,
            MissionTitle = missionTitle.Trim(),
            MissionType = missionType,
            Order = order,
            QrToken = qrToken,
            Latitude = latitude,
            Longitude = longitude
        };
    }

    public bool ValidateQrToken(Guid scannedStageId, string scannedToken)
        => MissionStageId == scannedStageId && QrToken == scannedToken;
}
