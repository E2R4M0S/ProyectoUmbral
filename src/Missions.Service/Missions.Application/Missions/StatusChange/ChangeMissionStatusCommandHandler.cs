using MediatR;
using Microsoft.Extensions.Logging;
using Missions.Application.Common.Interfaces;
using Missions.Application.Missions.StatusChange.Chain;
using Missions.Domain.Enums;

namespace Missions.Application.Missions.StatusChange;

public class ChangeMissionStatusCommandHandler : IRequestHandler<ChangeMissionStatusCommand>
{
    private readonly IMissionRepository _repository;
    private readonly IMissionLockService _lockService;
    private readonly IMissionStageValidator _stageValidator;
    private readonly ILogger<ChangeMissionStatusCommandHandler> _logger;
    private readonly IMissionStatusHandler _validationChain;

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

        // Build the Chain of Responsibility
        var validStatus = new ValidMissionStatusHandler();
        var requiresStages = new DraftToActiveRequiresStagesHandler();
        var requiresClues = new EachStageRequiresClueHandler();
        validStatus.SetNext(requiresStages);
        requiresStages.SetNext(requiresClues);
        _validationChain = validStatus;
    }

    public async Task Handle(ChangeMissionStatusCommand command, CancellationToken ct)
    {
        var mission = await _repository.GetByIdAsync(command.Id, ct);
        if (mission is null)
        {
            throw new InvalidOperationException($"Mission with id '{command.Id}' not found");
        }

        // Run the validation chain
        _validationChain.Handle(mission, command.Status);

        var newStatus = Enum.Parse<MissionStatus>(command.Status);

        if (await _lockService.IsMissionInUseAsync(command.Id, ct))
        {
            throw new InvalidOperationException($"Mission with id '{command.Id}' is in use");
        }

        mission.SetStatus(newStatus);

        await _repository.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Mission status changed: Id={MissionId}, Status={Status}",
            mission.Id, mission.Status);
    }
}
