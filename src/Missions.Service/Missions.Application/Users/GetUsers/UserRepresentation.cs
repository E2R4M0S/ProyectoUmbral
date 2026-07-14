using System.Collections.Immutable;

namespace Missions.Application.Users.GetUsers;

public sealed class UserRepresentation
{
    public string Id { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public long CreatedTimestamp { get; set; }
    public bool EmailVerified { get; set; }
    public ImmutableDictionary<string, string[]> Attributes { get; set; }
        = ImmutableDictionary<string, string[]>.Empty;
}
