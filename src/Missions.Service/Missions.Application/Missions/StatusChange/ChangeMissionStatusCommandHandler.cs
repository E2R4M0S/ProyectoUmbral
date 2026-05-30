using MediatR;
using Microsoft.Extensions.Logging;
using Missions.Application.Common.Interfaces;
using Missions.Application.Missions.StatusChange;
using Missions.Domain.Entities;
using Missions.Domain.Enums;

namespace Missions.Application.Missions.StatusChange;

public class ChangeMissionStatusCommandHandler : IRequestHandler<ChangeMissionStatusCommand>
{
    private readonly IMissionRepository _repository;
    private readonly IMissionLockService _lockService;
    private readonly IMissionStageValidator _stageValidator;
    private readonly ILogger<ChangeMissionStatusCommandHandler> _logger;

    public ChangeMissionStatusCommandHandler(
        IMissionRepository repository,
        IMissionLockService lockService,
        IMissionStageValidator stageValidator,
        ILogger<ChangeMissionStatusCommandHandler> logger)
    {
        _repository = repository;
        _lockService = lockService;
        _stageValidator = stageValidator;
        _logger = logger;
    }

    public async Task Handle(ChangeMissionStatusCommand command, CancellationToken ct)
    {
        var mission = await _repository.GetByIdAsync(command.Id, ct);
        if (mission is null)
        {
            throw new InvalidOperationException($"Mission with id '{command.Id}' not found");
        }

        var newStatus = Enum.Parse<MissionStatus>(command.Status);

        if (newStatus == MissionStatus.Active &&
            !await _stageValidator.HasAtLeastOneStageAsync(command.Id, ct))
        {
            throw new InvalidOperationException($"La misión debe tener al menos una etapa para ser activada");
        }

        if (await _lockService.IsMissionInUseAsync(command.Id, ct))
        {
            throw new InvalidOperationException($"Mission with id '{command.Id}' is in use");
        }

        mission.SetStatus(newStatus);

        await _repository.UpdateAsync(mission, ct);

        _logger.LogInformation(
            "Mission status changed: Id={MissionId}, Status={Status}",
            mission.Id, mission.Status);
    }
}