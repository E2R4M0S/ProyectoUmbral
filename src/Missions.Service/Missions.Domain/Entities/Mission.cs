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
}
