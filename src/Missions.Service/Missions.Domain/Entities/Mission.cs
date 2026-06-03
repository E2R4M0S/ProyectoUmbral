using Missions.Domain.Enums;

namespace Missions.Domain.Entities;

public class Mission : IMissionComponent
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public Difficulty Difficulty { get; private set; }
    public int TimeMinutes { get; private set; }
    public MissionType Type { get; private set; }
    public MissionStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private readonly List<MissionStage> _stages = new();
    public IReadOnlyList<MissionStage> Stages => _stages.AsReadOnly();

    private Mission() { } // EF Core

    public static Mission Create(
        string title,
        string description,
        Difficulty difficulty,
        int timeMinutes,
        MissionType type)
    {
        return new Mission
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            Description = description.Trim(),
            Difficulty = difficulty,
            TimeMinutes = timeMinutes,
            Type = type,
            Status = MissionStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string title, string description, Difficulty difficulty, int timeMinutes)
    {
        Title = title.Trim();
        Description = description.Trim();
        Difficulty = difficulty;
        TimeMinutes = timeMinutes;
    }

    public void SetStatus(MissionStatus newStatus)
    {
        if (Status == newStatus)
        {
            throw new InvalidOperationException($"Mission is already in '{Status}' status");
        }

        if (Status == MissionStatus.Draft && newStatus == MissionStatus.Inactive)
        {
            throw new InvalidOperationException("Cannot transition mission from 'Draft' to 'Inactive'");
        }

        Status = newStatus;
    }

    public void AddStage(string name, string description, int order)
    {
        if (_stages.Any(s => s.Order == order))
        {
            throw new InvalidOperationException($"Ya existe una etapa con el orden {order}");
        }

        if (_stages.Any(s => s.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Ya existe una etapa con el nombre '{name.Trim()}'");
        }

        var stage = new MissionStage(Id, name, description, order);
        _stages.Add(stage);
    }

    public void UpdateStage(Guid stageId, string name, string description, int order)
    {
        var stage = _stages.FirstOrDefault(s => s.Id == stageId);
        if (stage is null)
        {
            throw new InvalidOperationException($"Stage with id '{stageId}' not found");
        }

        if (_stages.Any(s => s.Id != stageId && s.Order == order))
        {
            throw new InvalidOperationException($"Ya existe una etapa con el orden {order}");
        }

        if (_stages.Any(s => s.Id != stageId && s.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Ya existe una etapa con el nombre '{name.Trim()}'");
        }

        stage.Update(name, description, order);
    }

    public void AddStageClue(Guid stageId, string content, int? penalty, ReleaseType releaseType)
    {
        var stage = _stages.FirstOrDefault(s => s.Id == stageId);
        if (stage is null)
        {
            throw new InvalidOperationException($"Stage with id '{stageId}' not found");
        }

        stage.AddClue(content, penalty, releaseType);
    }

    public void RemoveStageClue(Guid stageId, Guid clueId)
    {
        var stage = _stages.FirstOrDefault(s => s.Id == stageId);
        if (stage is null)
        {
            throw new InvalidOperationException($"Stage with id '{stageId}' not found");
        }

        stage.RemoveClue(clueId);
    }

    public ValidationResult Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Title))
        {
            errors.Add("Mission title cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(Description))
        {
            errors.Add("Mission description cannot be empty");
        }

        if (_stages.Count == 0)
        {
            errors.Add("Mission requires at least one stage");
        }

        foreach (var stage in _stages)
        {
            var stageResult = stage.Validate();
            if (!stageResult.IsValid)
            {
                errors.AddRange(stageResult.Errors);
            }
        }

        return errors.Count > 0
            ? new ValidationResult(errors)
            : ValidationResult.Success();
    }

    public int GetTotalPenalty() => _stages.Sum(s => s.GetTotalPenalty());

    public int GetLeafCount() => _stages.Sum(s => s.GetLeafCount());

}
