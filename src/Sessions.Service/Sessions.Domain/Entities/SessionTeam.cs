namespace Sessions.Domain.Entities;

public class SessionTeam
{
    private List<SessionTeamMember> _members = [];

    public Guid Id { get; private set; }
    public Guid SessionId { get; private set; }
    public string Name { get; private set; } = null!;
    public int MaxMembers { get; private set; }
    public int Score { get; private set; }
    public DateTime CreatedAt { get; private set; }
    // RB-08: ranking tie-break — when the score last changed, so ties can be broken by
    // whichever team reached that score first.
    public DateTime? LastScoreAt { get; private set; }
    public IReadOnlyList<SessionTeamMember> Members => _members.AsReadOnly();

    private SessionTeam() { }

    public static SessionTeam Create(Guid sessionId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("El nombre del equipo es obligatorio.");
        if (name.Length > 100)
            throw new InvalidOperationException("El nombre del equipo no puede superar los 100 caracteres.");

        return new SessionTeam
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Name = name.Trim(),
            MaxMembers = 5,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void AddMember(Guid userId, string? userAlias)
    {
        if (_members.Count >= MaxMembers)
            throw new InvalidOperationException($"El equipo ya tiene el máximo de {MaxMembers} miembros.");
        if (_members.Any(m => m.UserId == userId))
            throw new InvalidOperationException("El participante ya es miembro de este equipo.");

        _members.Add(SessionTeamMember.Create(Id, userId, userAlias));
    }

    public void AddScore(int delta) { if (delta > 0) { Score += delta; LastScoreAt = DateTime.UtcNow; } }

    public void ApplyPenalty(int amount) { if (amount > 0) { Score = Math.Max(0, Score - amount); LastScoreAt = DateTime.UtcNow; } }

    public void ResetScore() { Score = 0; LastScoreAt = null; }

    public void RemoveMember(Guid userId)
    {
        var member = _members.FirstOrDefault(m => m.UserId == userId)
            ?? throw new InvalidOperationException("El participante no es miembro de este equipo.");
        _members.Remove(member);
    }
}
