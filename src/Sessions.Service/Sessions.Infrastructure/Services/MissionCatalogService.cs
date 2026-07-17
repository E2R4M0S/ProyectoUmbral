using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sessions.Application.Common.Interfaces;

namespace Sessions.Infrastructure.Services;

public class MissionCatalogService : IMissionCatalogService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MissionCatalogService> _logger;

    public MissionCatalogService(HttpClient httpClient, ILogger<MissionCatalogService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<MissionSummary?> GetMissionAsync(Guid missionId, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync($"/{missionId}", ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Missions.Service returned {StatusCode} for mission {MissionId}",
                response.StatusCode, missionId);
            return null;
        }

        var mission = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var title = mission.TryGetProperty("title", out var t) ? t.GetString() ?? string.Empty : string.Empty;
        var status = mission.TryGetProperty("status", out var s) ? s.GetString() ?? string.Empty : string.Empty;
        var difficulty = mission.TryGetProperty("difficulty", out var d) ? d.GetString() ?? "Medium" : "Medium";
        return new MissionSummary(missionId, title, status, difficulty);
    }

    public async Task<IReadOnlyList<AutomaticClueSummary>> GetAutomaticCluesAsync(
        Guid missionId, Guid missionStageId, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync($"/{missionId}", ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Missions.Service returned {StatusCode} for mission {MissionId}",
                response.StatusCode, missionId);
            return Array.Empty<AutomaticClueSummary>();
        }

        var mission = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        if (!mission.TryGetProperty("stages", out var stages))
            return Array.Empty<AutomaticClueSummary>();

        var result = new List<AutomaticClueSummary>();
        foreach (var stage in stages.EnumerateArray())
        {
            if (!stage.TryGetProperty("id", out var stageId) || stageId.GetGuid() != missionStageId)
                continue;

            if (stage.TryGetProperty("clues", out var clues))
            {
                foreach (var clue in clues.EnumerateArray())
                {
                    var releaseType = clue.TryGetProperty("releaseType", out var rt) ? rt.GetString() : null;
                    if (!string.Equals(releaseType, "Automatic", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var id = clue.TryGetProperty("id", out var cid) ? cid.GetGuid() : Guid.Empty;
                    var content = clue.TryGetProperty("content", out var c) ? c.GetString() ?? string.Empty : string.Empty;
                    var penalty = clue.TryGetProperty("penalty", out var p) && p.ValueKind != JsonValueKind.Null
                        ? p.GetInt32() : (int?)null;

                    if (id != Guid.Empty)
                        result.Add(new AutomaticClueSummary(id, content, penalty));
                }
            }
            break;
        }

        return result;
    }
}
