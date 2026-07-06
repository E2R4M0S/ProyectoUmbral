using Missions.Domain.Enums;

namespace Missions.Domain.Entities;

public class MissionStage : IMissionComponent
{
    public Guid Id { get; private set; }
    public Guid MissionId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public int Order { get; private set; }
    public string QrToken { get; private set; } = null!;
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }

    public Mission Mission { get; private set; } = null!;

    private readonly List<MissionClue> _clues = new();
    public IReadOnlyList<MissionClue> Clues => _clues.AsReadOnly();

    private MissionStage() { } // EF Core

    internal void Update(string name, string description, int order, double? latitude = null, double? longitude = null)
    {
        Name = name.Trim();
        Description = description.Trim();
        Order = order;
        Latitude = latitude;
        Longitude = longitude;
    }

    internal MissionStage(Guid missionId, string name, string description, int order, double? latitude = null, double? longitude = null)
    {
        Id = Guid.NewGuid();
        MissionId = missionId;
        Name = name.Trim();
        Description = description.Trim();
        Order = order;
        QrToken = Guid.NewGuid().ToString("N");
        Latitude = latitude;
        Longitude = longitude;
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

    public ValidationResult Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add("Stage name cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(Description))
        {
            errors.Add("Stage description cannot be empty");
        }

        if (_clues.Count == 0)
        {
            errors.Add("Stage requires at least one clue");
        }

        foreach (var clue in _clues)
        {
            var clueResult = clue.Validate();
            if (!clueResult.IsValid)
            {
                errors.AddRange(clueResult.Errors);
            }
        }

        return errors.Count > 0
            ? new ValidationResult(errors)
            : ValidationResult.Success();
    }

    public int GetTotalPenalty() => _clues.Sum(c => c.GetTotalPenalty());

    public int GetLeafCount() => _clues.Sum(c => c.GetLeafCount());
}
