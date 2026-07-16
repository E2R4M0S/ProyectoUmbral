using MediatR;
using Microsoft.Extensions.Logging;
using Missions.Application.Common.Interfaces;
using Missions.Application.Missions.Stages;

namespace Missions.Application.Missions.Stages;

public class UpdateStageCommandHandler : IRequestHandler<UpdateStageCommand, UpdateStageCommandResult>
{
    private readonly IMissionRepository _repository;
    private readonly IMissionLockService _lockService;
    private readonly ILogger<UpdateStageCommandHandler> _logger;

    public UpdateStageCommandHandler(
        IMissionRepository repository,
        IMissionLockService lockService,
        ILogger<UpdateStageCommandHandler> logger)
    {
        _repository = repository;
        _lockService = lockService;
        _logger = logger;
    }

    public async Task<UpdateStageCommandResult> Handle(UpdateStageCommand command, CancellationToken ct)
    {
        var mission = await _repository.GetByIdAsync(command.MissionId, ct);
        if (mission is null)
        {
            throw new InvalidOperationException($"Mission with id '{command.MissionId}' not found");
        }

        if (await _lockService.IsMissionInUseAsync(command.MissionId, ct))
        {
            throw new InvalidOperationException($"Mission with id '{command.MissionId}' is in use");
        }

        mission.UpdateStage(command.StageId, command.Name, command.Description, command.Order, command.Latitude, command.Longitude);

        await _repository.UpdateAsync(mission, ct);

        var updatedStage = mission.Stages.Single(s => s.Id == command.StageId);

        _logger.LogInformation(
            "Stage updated: Id={StageId}, MissionId={MissionId}, Order={Order}",
            updatedStage.Id, mission.Id, updatedStage.Order);

        return new UpdateStageCommandResult(
            updatedStage.Id,
            updatedStage.Name,
            updatedStage.Description,
            updatedStage.Order,
            updatedStage.Latitude,
            updatedStage.Longitude);
    }
}
