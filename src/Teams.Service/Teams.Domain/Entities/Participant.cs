namespace Teams.Domain.Entities;

public class Participant
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string Alias { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string KeycloakUserId { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    private Participant() { } // EF Core

    public static Participant Create(string name, string alias, string email, string keycloakUserId)
    {
        // Validate via value objects (defense in depth)
        ValueObjects.Alias.Create(alias);
        ValueObjects.Email.Create(email);

        return new Participant
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Alias = alias.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            KeycloakUserId = keycloakUserId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string name, string alias)
    {
        var trimmedAlias = alias.Trim();
        ValueObjects.Alias.Create(trimmedAlias); // validates via VO
        Name = name.Trim();
        Alias = trimmedAlias;
    }
}
