namespace Missions.Application.Missions.Clues;

public record CreateClueCommandResult(
    Guid Id,
    string Content,
    int? Penalty,
    string ReleaseType
);
