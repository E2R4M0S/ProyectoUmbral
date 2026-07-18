using MediatR;

namespace Sessions.Application.Sessions.Transition;

public record TransitionSessionCommand(
    Guid Id,
    string NewStatus,
    // True when the transition is decided by game logic (e.g. every participant finished
    // the last stage), not requested by the owning operator through the UI. The RB-10
    // ownership check only makes sense for operator-initiated transitions — a system-driven
    // one can run inside another user's (a participant's) HTTP request, whose identity has
    // nothing to do with who owns the session.
    bool SkipOwnershipCheck = false) : IRequest;