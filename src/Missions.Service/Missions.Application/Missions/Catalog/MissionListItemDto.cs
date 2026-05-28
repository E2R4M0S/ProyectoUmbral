namespace Missions.Application.Missions.Catalog;

public record MissionListItemDto(
    Guid Id,
    string Title,
    string Difficulty,
    string Type,
    string Status
);