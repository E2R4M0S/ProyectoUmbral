namespace Teams.Domain.ValueObjects;

public record Email
{
    public string Value { get; }

    private Email(string value) => Value = value;

    public static Email Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email cannot be empty", nameof(value));

        if (!value.Contains('@') || !value.Contains('.'))
            throw new ArgumentException("Invalid email format", nameof(value));

        return new Email(value.Trim().ToLowerInvariant());
    }

    public static implicit operator string(Email email) => email.Value;
}
