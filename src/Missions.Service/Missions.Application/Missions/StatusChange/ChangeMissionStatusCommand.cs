using MediatR;

namespace Missions.Application.Missions.StatusChange;

public record ChangeMissionStatusCommand(
    Guid Id,
    string Status) : IRequest;