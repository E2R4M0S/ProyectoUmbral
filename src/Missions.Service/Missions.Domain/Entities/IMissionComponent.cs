namespace Missions.Domain.Entities;

public interface IMissionComponent
{
    Guid Id { get; }
    ValidationResult Validate();
    int GetTotalPenalty();
    int GetLeafCount();
}