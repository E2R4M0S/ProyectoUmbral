using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Missions.Application.Common.Interfaces;
using Missions.Application.Missions.Stages;
using Missions.Domain.Entities;
using Missions.Domain.Enums;
using Xunit;

namespace Missions.Application.Tests.Missions.Stages;

public class DeleteStageCommandHandlerTests
{
    private readonly IMissionRepository _repository = Substitute.For<IMissionRepository>();
    private readonly ILogger<DeleteStageCommandHandler> _logger =
        Substitute.For<ILogger<DeleteStageCommandHandler>>();
    private readonly DeleteStageCommandHandler _sut;

    public DeleteStageCommandHandlerTests()
    {
        _sut = new DeleteStageCommandHandler(_repository, _logger);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldDeleteStageAndReturnUnit()
    {
        // Arrange
        var mission = Mission.Create("Test Mission", "Description", Difficulty.Easy, 30, MissionType.Treasure);
        mission.AddStage("Stage One", "First stage description", 1);
        var stageId = mission.Stages.First().Id;
        var command = new DeleteStageCommand(mission.Id, stageId);

        _repository.GetByIdAsync(mission.Id, Arg.Any<CancellationToken>())
            .Returns(mission);
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act & Assert - should complete without throwing and return Unit.Value implicitly
        await _sut.Handle(command, CancellationToken.None);

        _repository.Received(1).RemoveStage(mission, Arg.Any<MissionStage>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenMissionNotFound_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var missionId = Guid.NewGuid();
        var command = new DeleteStageCommand(missionId, Guid.NewGuid());

        _repository.GetByIdAsync(missionId, Arg.Any<CancellationToken>())
            .Returns((Mission?)null);

        // Act
        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Mission not found*");

        await _repository.Received(1).GetByIdAsync(missionId, Arg.Any<CancellationToken>());
        _repository.DidNotReceive().RemoveStage(Arg.Any<Mission>(), Arg.Any<MissionStage>());
    }

    [Fact]
    public async Task Handle_WhenStageNotFound_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var mission = Mission.Create("Test Mission", "Description", Difficulty.Easy, 30, MissionType.Treasure);
        var command = new DeleteStageCommand(mission.Id, Guid.NewGuid());

        _repository.GetByIdAsync(mission.Id, Arg.Any<CancellationToken>())
            .Returns(mission);

        // Act
        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Stage not found*");

        _repository.DidNotReceive().RemoveStage(Arg.Any<Mission>(), Arg.Any<MissionStage>());
    }

    [Fact]
    public async Task Handle_WhenMissionIsActive_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var mission = Mission.Create("Test Mission", "Description", Difficulty.Easy, 30, MissionType.Treasure);
        mission.AddStage("Stage One", "First stage description", 1);
        mission.SetStatus(MissionStatus.Active);
        var stageId = mission.Stages.First().Id;
        var command = new DeleteStageCommand(mission.Id, stageId);

        _repository.GetByIdAsync(mission.Id, Arg.Any<CancellationToken>())
            .Returns(mission);

        // Act
        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No se puede eliminar una etapa de una misión activa*");

        _repository.DidNotReceive().RemoveStage(Arg.Any<Mission>(), Arg.Any<MissionStage>());
    }
}