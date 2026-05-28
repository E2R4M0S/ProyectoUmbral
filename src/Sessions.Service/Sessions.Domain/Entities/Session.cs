using Sessions.Domain.Enums;

namespace Sessions.Domain.Entities;

public class Session
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public Guid MissionId { get; private set; }
    public string Pin { get; private set; } = null!;
    public SessionStatus Status { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? EndedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Session() { } // EF Core

    public static Session Create(string name, Guid missionId, string pin)
    {
        return new Session
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            MissionId = missionId,
            Pin = pin,
            Status = SessionStatus.Scheduled,
            CreatedAt = DateTime.UtcNow
        };
    }
}
