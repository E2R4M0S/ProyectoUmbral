using MediatR;
using Microsoft.Extensions.Logging;
using Missions.Application.Common.Interfaces;
using Missions.Application.Missions.Clues;
using Missions.Domain.Enums;

namespace Missions.Application.Missions.Clues;

public class CreateClueCommandHandler : IRequestHandler<CreateClueCommand, CreateClueCommandResult>
{
    private readonly IMissionRepository _repository;
    private readonly ILogger<CreateClueCommandHandler> _logger;

    public CreateClueCommandHandler(
        IMissionRepository repository,
        ILogger<CreateClueCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<CreateClueCommandResult> Handle(
        CreateClueCommand command,
        CancellationToken ct)
    {
        var mission = await _repository.GetByIdAsync(command.MissionId, ct);

        if (mission is null)
        {
            throw new InvalidOperationException($"Mission with id '{command.MissionId}' not found");
        }

        var releaseType = Enum.Parse<ReleaseType>(command.ReleaseType);

        mission.AddStageClue(command.StageId, command.Content, command.Penalty, releaseType);

        await _repository.UpdateAsync(mission, ct);

        var stage = mission.Stages.First(s => s.Id == command.StageId);
        var clue = stage.Clues.Last();

        _logger.LogInformation(
            "Clue created: Id={ClueId}, StageId={StageId}, MissionId={MissionId}",
            clue.Id, command.StageId, command.MissionId);

        return new CreateClueCommandResult(
            clue.Id,
            clue.Content,
            clue.Penalty,
            clue.ReleaseType.ToString());
    }
}
