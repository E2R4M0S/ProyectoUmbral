using MediatR;
using Microsoft.Extensions.Logging;
using Missions.Application.Common.Interfaces;

namespace Missions.Application.Missions.Stages;

public class DeleteStageCommandHandler : IRequestHandler<DeleteStageCommand>
{
    private readonly IMissionRepository _repository;
    private readonly ILogger<DeleteStageCommandHandler> _logger;

    public DeleteStageCommandHandler(
        IMissionRepository repository,
        ILogger<DeleteStageCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Handle(DeleteStageCommand command, CancellationToken ct)
    {
        var mission = await _repository.GetByIdAsync(command.MissionId, ct)
            ?? throw new InvalidOperationException($"Mission not found");

        var stage = mission.Stages.FirstOrDefault(s => s.Id == command.StageId)
            ?? throw new InvalidOperationException($"Stage not found");

        if (mission.Status == Domain.Enums.MissionStatus.Active)
            throw new InvalidOperationException("No se puede eliminar una etapa de una misión activa");

        // Remove stage from the collection via EF change tracker
        _repository.RemoveStage(mission, stage);
        await _repository.SaveChangesAsync(ct);

        _logger.LogInformation("Stage deleted: Id={StageId}, MissionId={MissionId}", command.StageId, command.MissionId);
    }
}
