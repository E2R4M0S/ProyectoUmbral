using Missions.Domain.Entities;

namespace Missions.Application.Missions.StatusChange.Chain;

public abstract class BaseMissionStatusHandler : IMissionStatusHandler
{
    private IMissionStatusHandler? _next;

    public IMissionStatusHandler SetNext(IMissionStatusHandler handler)
    {
        _next = handler;
        return handler;
    }

    public void Handle(Mission mission, string newStatus)
    {
        Validate(mission, newStatus);
        _next?.Handle(mission, newStatus);
    }

    protected abstract void Validate(Mission mission, string newStatus);
}
