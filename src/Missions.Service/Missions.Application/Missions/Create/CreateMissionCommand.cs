using MediatR;
using Missions.Application.Missions.Create;

namespace Missions.Application.Missions.Create;

public record CreateMissionCommand(
    string Title,
    string Description,
    string Difficulty,
    int TimeMinutes,
    string Type) : IRequest<CreateMissionCommandResult>;