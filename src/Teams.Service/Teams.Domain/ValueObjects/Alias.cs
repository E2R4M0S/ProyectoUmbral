using System.Text.RegularExpressions;

namespace Teams.Domain.ValueObjects;

public partial record Alias
{
    public string Value { get; }

    private Alias(string value) => Value = value;

    public static Alias Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Alias cannot be empty", nameof(value));

        if (value.Length < 3 || value.Length > 50)
            throw new ArgumentException("Alias must be between 3 and 50 characters", nameof(value));

        if (!AliasRegex().IsMatch(value))
            throw new ArgumentException("Alias must be alphanumeric with underscores only", nameof(value));

        return new Alias(value.Trim());
    }

    public static implicit operator string(Alias alias) => alias.Value;

    [GeneratedRegex(@"^[a-zA-Z0-9_]+$")]
    private static partial Regex AliasRegex();
}
