namespace Missions.Application.Missions.Clues;

public record ClueDto(
    Guid Id,
    string Content,
    int? Penalty,
    string ReleaseType
);
