using MediatR;
using Microsoft.Extensions.Logging;
using Missions.Application.Common.Interfaces;
using Missions.Application.Missions.Stages;

namespace Missions.Application.Missions.Stages;

public class CreateStageCommandHandler : IRequestHandler<CreateStageCommand, CreateStageCommandResult>
{
    private readonly IMissionRepository _repository;
    private readonly ILogger<CreateStageCommandHandler> _logger;

    public CreateStageCommandHandler(
        IMissionRepository repository,
        ILogger<CreateStageCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<CreateStageCommandResult> Handle(
        CreateStageCommand command,
        CancellationToken ct)
    {
        var mission = await _repository.GetByIdAsync(command.MissionId, ct);

        if (mission is null)
        {
            throw new InvalidOperationException($"Mission with id '{command.MissionId}' not found");
        }

        try
        {
            mission.AddStage(command.Name, command.Description, command.Order);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            throw;
        }

        await _repository.UpdateAsync(mission, ct);

        var stage = mission.Stages.Last();

        _logger.LogInformation(
            "Stage created: Id={StageId}, MissionId={MissionId}, Order={Order}",
            stage.Id, mission.Id, stage.Order);

        return new CreateStageCommandResult(
            stage.Id,
            stage.Name,
            stage.Description,
            stage.Order);
    }
}
