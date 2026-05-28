using MediatR;
using Microsoft.Extensions.Logging;
using Missions.Application.Common.Interfaces;
using Missions.Application.Missions.Update;
using Missions.Domain.Entities;
using Missions.Domain.Enums;

namespace Missions.Application.Missions.Update;

public class UpdateMissionCommandHandler : IRequestHandler<UpdateMissionCommand>
{
    private readonly IMissionRepository _repository;
    private readonly IMissionLockService _lockService;
    private readonly ILogger<UpdateMissionCommandHandler> _logger;

    public UpdateMissionCommandHandler(
        IMissionRepository repository,
        IMissionLockService lockService,
        ILogger<UpdateMissionCommandHandler> logger)
    {
        _repository = repository;
        _lockService = lockService;
        _logger = logger;
    }

    public async Task Handle(UpdateMissionCommand command, CancellationToken ct)
    {
        var difficulty = Enum.Parse<Difficulty>(command.Difficulty);

        var mission = await _repository.GetByIdAsync(command.Id, ct);
        if (mission is null)
        {
            throw new InvalidOperationException($"Mission with id '{command.Id}' not found");
        }

        if (await _lockService.IsMissionInUseAsync(command.Id, ct))
        {
            throw new InvalidOperationException($"Mission with id '{command.Id}' is in use");
        }

        if (!await _repository.IsTitleUniqueAsync(command.Title, ct, command.Id))
        {
            throw new InvalidOperationException($"Mission with title '{command.Title}' already exists");
        }

        mission.Update(command.Title, command.Description, difficulty, command.TimeMinutes);

        await _repository.UpdateAsync(mission, ct);

        _logger.LogInformation(
            "Mission updated: Id={MissionId}, Title={Title}",
            mission.Id, mission.Title);
    }
}
