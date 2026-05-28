namespace Missions.Application.Missions.Create;

public record CreateMissionCommandResult(
    Guid Id,
    string Title,
    string Description,
    string Difficulty,
    int TimeMinutes,
    string Type,
    string Status,
    DateTime CreatedAt);