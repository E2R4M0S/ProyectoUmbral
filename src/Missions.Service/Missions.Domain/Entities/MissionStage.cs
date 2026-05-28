using Missions.Domain.Enums;

namespace Missions.Domain.Entities;

public class MissionStage
{
    public Guid Id { get; private set; }
    public Guid MissionId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public int Order { get; private set; }

    public Mission Mission { get; private set; } = null!;

    private readonly List<MissionClue> _clues = new();
    public IReadOnlyList<MissionClue> Clues => _clues.AsReadOnly();

    private MissionStage() { } // EF Core

    internal void Update(string name, string description, int order)
    {
        Name = name.Trim();
        Description = description.Trim();
        Order = order;
    }

    internal MissionStage(Guid missionId, string name, string description, int order)
    {
        Id = Guid.NewGuid();
        MissionId = missionId;
        Name = name.Trim();
        Description = description.Trim();
        Order = order;
    }

    internal void AddClue(string content, int? penalty, ReleaseType releaseType)
    {
        var clue = new MissionClue(Id, content, penalty, releaseType);
        _clues.Add(clue);
    }

    internal void RemoveClue(Guid clueId)
    {
        var clue = _clues.FirstOrDefault(c => c.Id == clueId);
        if (clue is null)
        {
            throw new InvalidOperationException($"Clue with id '{clueId}' not found");
        }
        _clues.Remove(clue);
    }
}
