using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Missions.Application.Common.Interfaces;
using Missions.Application.Missions.Clues;
using Missions.Domain.Entities;
using Missions.Domain.Enums;
using Xunit;

namespace Missions.Application.Tests.Missions.Clues;

public class DeleteClueCommandHadlerTests
{
    private readonly IMissionRepository _repository = Substitute.For<IMissionRepository>();
    private readonly IMissionLockService _lockService = Substitute.For<IMissionLockService>();
    private readonly ILogger<DeleteClueCommandHandler> _logger =
        Substitute.For<ILogger<DeleteClueCommandHandler>>();
    private readonly DeleteClueCommandHandler _sut;

    public DeleteClueCommandHadlerTests()
    {
        _sut = new DeleteClueCommandHandler(_repository, _lockService, _logger);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldDeleteClueAndReturnTrue()
    {
        // Arrange
        var mission = Mission.Create("Test Mission", "Description", Difficulty.Easy, 30, MissionType.Treasure);
        mission.AddStage("Stage One", "First stage", 1);
        var stageId = mission.Stages.Single().Id;
        mission.AddStageClue(stageId, "Clue to delete", null, ReleaseType.Auto);
        var clueId = mission.Stages.Single().Clues.Single().Id;
        var command = new DeleteClueCommand(mission.Id, stageId, clueId);

        _repository.GetByIdAsync(mission.Id, Arg.Any<CancellationToken>())
            .Returns(mission);
        _lockService.IsMissionInUseAsync(mission.Id, Arg.Any<CancellationToken>())
            .Returns(false);
        _repository.UpdateAsync(Arg.Any<Mission>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        mission.Stages.Single().Clues.Should().BeEmpty();

        await _repository.Received(1).GetByIdAsync(mission.Id, Arg.Any<CancellationToken>());
        await _lockService.Received(1).IsMissionInUseAsync(mission.Id, Arg.Any<CancellationToken>());
        await _repository.Received(1).UpdateAsync(Arg.Any<Mission>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenMissionNotFound_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var missionId = Guid.NewGuid();
        var command = new DeleteClueCommand(missionId, Guid.NewGuid(), Guid.NewGuid());

        _repository.GetByIdAsync(missionId, Arg.Any<CancellationToken>())
            .Returns((Mission?)null);

        // Act
        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*not found*");
    }

    [Fact]
    public async Task Handle_WhenClueNotFound_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var mission = Mission.Create("Test Mission", "Description", Difficulty.Easy, 30, MissionType.Treasure);
        mission.AddStage("Stage One", "First stage", 1);
        var stageId = mission.Stages.Single().Id;
        var wrongClueId = Guid.NewGuid();
        var command = new DeleteClueCommand(mission.Id, stageId, wrongClueId);

        _repository.GetByIdAsync(mission.Id, Arg.Any<CancellationToken>())
            .Returns(mission);
        _lockService.IsMissionInUseAsync(mission.Id, Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*not found*");
    }

    [Fact]
    public async Task Handle_WhenMissionInUse_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var mission = Mission.Create("Test Mission", "Description", Difficulty.Easy, 30, MissionType.Treasure);
        mission.AddStage("Stage One", "First stage", 1);
        var stageId = mission.Stages.Single().Id;
        mission.AddStageClue(stageId, "Clue", null, ReleaseType.Auto);
        var clueId = mission.Stages.Single().Clues.Single().Id;
        var command = new DeleteClueCommand(mission.Id, stageId, clueId);

        _repository.GetByIdAsync(mission.Id, Arg.Any<CancellationToken>())
            .Returns(mission);
        _lockService.IsMissionInUseAsync(mission.Id, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*is in use*");
    }
}
