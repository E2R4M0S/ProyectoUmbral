using MediatR;

namespace Sessions.Application.Sessions.Transition;

public record TransitionSessionCommand(
    Guid Id,
    string NewStatus) : IRequest;