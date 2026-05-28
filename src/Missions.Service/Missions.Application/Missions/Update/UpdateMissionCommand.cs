using MediatR;

namespace Missions.Application.Missions.Update;

public record UpdateMissionCommand(
    Guid Id,
    string Title,
    string Description,
    string Difficulty,
    int TimeMinutes) : IRequest;
