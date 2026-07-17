namespace Sessions.Application.Common.Interfaces;

public interface IMissionCatalogService
{
    Task<MissionSummary?> GetMissionAsync(Guid missionId, CancellationToken ct = default);

    // RF-07: automatic clue release conditioned by advance rules — clues whose
    // ReleaseType is "Automatic" for a given stage.
    Task<IReadOnlyList<AutomaticClueSummary>> GetAutomaticCluesAsync(
        Guid missionId, Guid missionStageId, CancellationToken ct = default);
}

public record MissionSummary(Guid Id, string Title, string Status, string Difficulty = "Medium");

public record AutomaticClueSummary(Guid Id, string Content, int? Penalty);
