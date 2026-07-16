using System.Text.Json.Serialization;
using MediatR;

namespace Sessions.Application.Sessions.Create;

public record CreateSessionCommand(
    string Name,
    List<StageInput> Stages) : IRequest<CreateSessionCommandResult>;

public record StageInput(
    [property: JsonPropertyName("missionId")] Guid MissionId,
    [property: JsonPropertyName("missionStageId")] Guid MissionStageId,
    [property: JsonPropertyName("missionTitle")] string MissionTitle,
    [property: JsonPropertyName("stageName")] string StageName,
    [property: JsonPropertyName("missionType")] string MissionType,
    [property: JsonPropertyName("order")] int Order,
    [property: JsonPropertyName("qrToken")] string QrToken,
    [property: JsonPropertyName("timeMinutes")] int TimeMinutes = 0,
    [property: JsonPropertyName("latitude")] double? Latitude = null,
    [property: JsonPropertyName("longitude")] double? Longitude = null);
