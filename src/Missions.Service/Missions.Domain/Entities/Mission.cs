using Missions.Domain.Enums;

namespace Missions.Domain.Entities;

public class Mission
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
            throw new InvalidOperationException($"A stage with Order {order} already exists");
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
            throw new InvalidOperationException($"A stage with Order {order} already exists");
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

}
