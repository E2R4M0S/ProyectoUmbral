using MediatR;
using Microsoft.Extensions.Logging;
using Missions.Application.Common.Interfaces;
using Missions.Application.Missions.Create;
using Missions.Domain.Entities;
using Missions.Domain.Enums;

namespace Missions.Application.Missions.Create;

public class CreateMissionCommandHandler
    : IRequestHandler<CreateMissionCommand, CreateMissionCommandResult>
{
    private readonly IMissionRepository _repository;
    private readonly ILogger<CreateMissionCommandHandler> _logger;

    public CreateMissionCommandHandler(
        IMissionRepository repository,
        ILogger<CreateMissionCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<CreateMissionCommandResult> Handle(
        CreateMissionCommand command,
        CancellationToken ct)
    {
        var difficulty = Enum.Parse<Difficulty>(command.Difficulty);
        var type = Enum.Parse<MissionType>(command.Type);

        if (!await _repository.IsTitleUniqueAsync(command.Title, ct))
        {
            throw new InvalidOperationException($"Title '{command.Title}' already exists");
        }

        var mission = Mission.Create(
            command.Title,
            command.Description,
            difficulty,
            command.TimeMinutes,
            type);

        await _repository.AddAsync(mission, ct);

        _logger.LogInformation(
            "Mission created: Id={MissionId}, Title={Title}",
            mission.Id, mission.Title);

        return new CreateMissionCommandResult(
            mission.Id,
            mission.Title,
            mission.Description,
            mission.Difficulty.ToString(),
            mission.TimeMinutes,
            mission.Type.ToString(),
            mission.Status.ToString(),
            mission.CreatedAt);
    }
}