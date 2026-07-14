namespace Teams.Domain.Entities;

public class Participant
{
    public Guid Id { get; private set; }
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string Username { get; private set; } = null!;
    public string Alias { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string KeycloakUserId { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    private Participant() { } // EF Core

    public static Participant Create(string firstName, string lastName, string username, string alias, string email, string keycloakUserId)
    {
        // Validate via value objects (defense in depth)
        ValueObjects.Alias.Create(alias);
        ValueObjects.Email.Create(email);

        return new Participant
        {
            Id = Guid.NewGuid(),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Username = username.Trim(),
            Alias = alias.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            KeycloakUserId = keycloakUserId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string firstName, string lastName, string alias)
    {
        var trimmedAlias = alias.Trim();
        ValueObjects.Alias.Create(trimmedAlias); // validates via VO
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Alias = trimmedAlias;
    }
}
