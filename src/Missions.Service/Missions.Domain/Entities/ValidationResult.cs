namespace Missions.Domain.Entities;

public class ValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public IReadOnlyList<string> Errors { get; }

    public ValidationResult()
    {
        Errors = Array.Empty<string>();
    }

    public ValidationResult(IReadOnlyList<string> errors)
    {
        Errors = errors;
    }

    public static ValidationResult Success() => new();

    public static ValidationResult Failure(string error) => new(new[] { error });

    public static ValidationResult Combine(params ValidationResult[] results)
    {
        var allErrors = new List<string>();
        foreach (var result in results)
        {
            foreach (var error in result.Errors)
            {
                allErrors.Add(error);
            }
        }
        return new ValidationResult(allErrors);
    }
}