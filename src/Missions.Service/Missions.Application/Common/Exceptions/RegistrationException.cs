namespace Missions.Application.Common.Exceptions;

public class RegistrationException : Exception
{
    public string Field { get; }

    public RegistrationException(string field, string message)
        : base(message)
    {
        Field = field;
    }
}
