namespace Teams.Domain.Entities;

public class Team
{
    private static readonly Random _random = new();

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public string LeaderId { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public string? JoinCode { get; private set; }

    private readonly List<TeamMember> _members = new();
    public IReadOnlyList<TeamMember> Members => _members.AsReadOnly();

    private Team() { }

    public static Team Create(string name, string description, string leaderId)
    {
        return new Team
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Description = description.Trim(),
            LeaderId = leaderId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void AddMember(string userId)
    {
        if (_members.Any(m => m.UserId == userId))
        {
            throw new InvalidOperationException($"User '{userId}' is already a member");
        }
        _members.Add(TeamMember.Create(Id, userId));
    }

    public void RemoveMember(string userId)
    {
        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if (member is null)
        {
            throw new InvalidOperationException($"User '{userId}' is not a member");
        }
        _members.Remove(member);
    }

    public void Update(string name, string description)
    {
        Name = name.Trim();
        Description = description.Trim();
    }

    public void GenerateJoinCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var code = new char[6];
        lock (_random)
        {
            for (int i = 0; i < 6; i++)
            {
                code[i] = chars[_random.Next(chars.Length)];
            }
        }
        JoinCode = new string(code);
    }
}
