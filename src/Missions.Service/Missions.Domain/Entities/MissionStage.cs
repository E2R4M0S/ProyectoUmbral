namespace Missions.Domain.Entities;

public class MissionStage
{
    public Guid Id { get; private set; }
    public Guid MissionId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public int Order { get; private set; }

    public Mission Mission { get; private set; } = null!;

    private MissionStage() { } // EF Core

    internal MissionStage(Guid missionId, string name, string description, int order)
    {
        Id = Guid.NewGuid();
        MissionId = missionId;
        Name = name.Trim();
        Description = description.Trim();
        Order = order;
    }
}
