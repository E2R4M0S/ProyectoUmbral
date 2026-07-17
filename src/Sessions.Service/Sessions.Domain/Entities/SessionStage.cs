namespace Sessions.Domain.Entities;

public class SessionStage
{
    public Guid MissionId { get; private set; }
    public Guid MissionStageId { get; private set; }
    public string MissionTitle { get; private set; } = null!;
    public string StageName { get; private set; } = null!;
    public string MissionType { get; private set; } = null!;
    public int Order { get; private set; }
    public string QrToken { get; private set; } = null!;
    public int TimeMinutes { get; private set; }
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }
    public string Difficulty { get; private set; } = "Medium";

    // RF: el puntaje base de cada escaneo QR depende de la dificultad de la misión.
    public int BaseScanPoints => Difficulty switch
    {
        "Easy" => 100,
        "Hard" => 200,
        _ => 150 // Medium (y cualquier valor desconocido)
    };

    private SessionStage() { }

    public static SessionStage Create(
        Guid missionId,
        Guid missionStageId,
        string missionTitle,
        string stageName,
        string missionType,
        int order,
        string qrToken,
        int timeMinutes = 0,
        double? latitude = null,
        double? longitude = null,
        string difficulty = "Medium")
    {
        if (missionId == Guid.Empty)
            throw new InvalidOperationException("SessionStage MissionId cannot be empty");
        if (string.IsNullOrWhiteSpace(missionTitle))
            throw new InvalidOperationException("SessionStage MissionTitle is required");
        if (string.IsNullOrWhiteSpace(stageName))
            throw new InvalidOperationException("SessionStage StageName is required");
        if (string.IsNullOrWhiteSpace(missionType))
            throw new InvalidOperationException("SessionStage MissionType is required");
        if (order < 1)
            throw new InvalidOperationException("SessionStage Order must be a positive integer");
        if (missionType == "Treasure" && missionStageId == Guid.Empty)
            throw new InvalidOperationException("SessionStage MissionStageId cannot be empty");
        if (missionType == "Treasure" && string.IsNullOrWhiteSpace(qrToken))
            throw new InvalidOperationException("SessionStage QrToken is required");

        return new SessionStage
        {
            MissionId = missionId,
            MissionStageId = missionStageId,
            MissionTitle = missionTitle.Trim(),
            StageName = stageName.Trim(),
            MissionType = missionType,
            Order = order,
            QrToken = qrToken,
            TimeMinutes = timeMinutes,
            Latitude = latitude,
            Longitude = longitude,
            Difficulty = string.IsNullOrWhiteSpace(difficulty) ? "Medium" : difficulty
        };
    }

    public bool ValidateQrToken(Guid scannedStageId, string scannedToken)
        => MissionStageId == scannedStageId && QrToken == scannedToken;
}
