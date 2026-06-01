using Missions.Domain.Enums;

namespace Missions.Domain.Entities;

public class MissionClue : IMissionComponent
{
    public Guid Id { get; private set; }
    public Guid StageId { get; private set; }
    public string Content { get; private set; } = null!;
    public int? Penalty { get; private set; }
    public ReleaseType ReleaseType { get; private set; }

    private MissionClue() { } // EF Core

    internal MissionClue(Guid stageId, string content, int? penalty, ReleaseType releaseType)
    {
        Id = Guid.NewGuid();
        StageId = stageId;
        Content = content.Trim();
        Penalty = penalty;
        ReleaseType = releaseType;
    }

    public ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Content))
        {
            return ValidationResult.Failure("Clue content cannot be empty");
        }
        return ValidationResult.Success();
    }

    public int GetTotalPenalty() => Penalty ?? 0;

    public int GetLeafCount() => 1;
}
