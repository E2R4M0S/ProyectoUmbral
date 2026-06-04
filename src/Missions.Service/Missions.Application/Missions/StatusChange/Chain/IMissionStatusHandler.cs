using Missions.Domain.Entities;

namespace Missions.Application.Missions.StatusChange.Chain;

/// <summary>
/// Chain of Responsibility for mission status change validation.
/// </summary>
public interface IMissionStatusHandler
{
    IMissionStatusHandler SetNext(IMissionStatusHandler handler);
    void Handle(Mission mission, string newStatus);
}
