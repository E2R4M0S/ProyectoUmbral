using MediatR;
using Microsoft.Extensions.Logging;
using Missions.Application.Common.Interfaces;
using Missions.Application.Missions.Clues;

namespace Missions.Application.Missions.Clues;

public class DeleteClueCommandHandler : IRequestHandler<DeleteClueCommand, bool>
{
    private readonly IMissionRepository _repository;
    private readonly IMissionLockService _lockService;
    private readonly ILogger<DeleteClueCommandHandler> _logger;

    public DeleteClueCommandHandler(
        IMissionRepository repository,
        IMissionLockService lockService,
        ILogger<DeleteClueCommandHandler> logger)
    {
        _repository = repository;
        _lockService = lockService;
        _logger = logger;
    }

    public async Task<bool> Handle(
        DeleteClueCommand command,
        CancellationToken ct)
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

        mission.RemoveStageClue(command.StageId, command.ClueId);

        await _repository.UpdateAsync(mission, ct);

        _logger.LogInformation(
            "Clue deleted: ClueId={ClueId}, StageId={StageId}, MissionId={MissionId}",
            command.ClueId, command.StageId, command.MissionId);

        return true;
    }
}
