using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Missions.Application.Common.Interfaces;
using Missions.Application.Missions.Stages;
using Missions.Domain.Entities;
using Missions.Domain.Enums;
using Xunit;

namespace Missions.Application.Tests.Missions.Stages;

public class UpdateStageCommandHandlerTests
{
    private readonly IMissionRepository _repository = Substitute.For<IMissionRepository>();
    private readonly IMissionLockService _lockService = Substitute.For<IMissionLockService>();
    private readonly ILogger<UpdateStageCommandHandler> _logger =
        Substitute.For<ILogger<UpdateStageCommandHandler>>();
    private readonly UpdateStageCommandHandler _sut;

    public UpdateStageCommandHandlerTests()
    {
        _sut = new UpdateStageCommandHandler(_repository, _lockService, _logger);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldUpdateStageAndReturnResult()
    {
        // Arrange
        var mission = Mission.Create("Test Mission", "Description", Difficulty.Easy, 30, MissionType.Treasure);
        mission.AddStage("Original Name", "Original Description", 1);
        var stageId = mission.Stages.Single().Id;
        var command = new UpdateStageCommand(mission.Id, stageId, "Updated Name", "Updated Description", 2);

        _repository.GetByIdAsync(mission.Id, Arg.Any<CancellationToken>())
            .Returns(mission);
        _lockService.IsMissionInUseAsync(mission.Id, Arg.Any<CancellationToken>())
            .Returns(false);
        _repository.UpdateAsync(Arg.Any<Mission>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Updated Name");
        result.Description.Should().Be("Updated Description");
        result.Order.Should().Be(2);

        await _repository.Received(1).GetByIdAsync(mission.Id, Arg.Any<CancellationToken>());
        await _lockService.Received(1).IsMissionInUseAsync(mission.Id, Arg.Any<CancellationToken>());
        await _repository.Received(1).UpdateAsync(Arg.Any<Mission>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenMissionNotFound_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var missionId = Guid.NewGuid();
        var stageId = Guid.NewGuid();
        var command = new UpdateStageCommand(missionId, stageId, "Name", "Description", 1);

        _repository.GetByIdAsync(missionId, Arg.Any<CancellationToken>())
            .Returns((Mission?)null);

        // Act
        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*not found*");

        await _repository.Received(1).GetByIdAsync(missionId, Arg.Any<CancellationToken>());
        await _lockService.DidNotReceive().IsMissionInUseAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenStageNotFound_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var mission = Mission.Create("Test Mission", "Description", Difficulty.Easy, 30, MissionType.Treasure);
        mission.AddStage("Stage One", "First stage", 1);
        var wrongStageId = Guid.NewGuid();
        var command = new UpdateStageCommand(mission.Id, wrongStageId, "Updated", "Description", 2);

        _repository.GetByIdAsync(mission.Id, Arg.Any<CancellationToken>())
            .Returns(mission);
        _lockService.IsMissionInUseAsync(mission.Id, Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*not found*");

        await _repository.Received(1).GetByIdAsync(mission.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenMissionInUse_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var mission = Mission.Create("Test Mission", "Description", Difficulty.Easy, 30, MissionType.Treasure);
        mission.AddStage("Stage One", "First stage", 1);
        var stageId = mission.Stages.Single().Id;
        var command = new UpdateStageCommand(mission.Id, stageId, "Updated", "Description", 2);

        _repository.GetByIdAsync(mission.Id, Arg.Any<CancellationToken>())
            .Returns(mission);
        _lockService.IsMissionInUseAsync(mission.Id, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*in use*");
    }

    [Fact]
    public async Task Handle_WithDuplicateOrder_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var mission = Mission.Create("Test Mission", "Description", Difficulty.Easy, 30, MissionType.Treasure);
        mission.AddStage("Stage One", "First stage", 1);
        mission.AddStage("Stage Two", "Second stage", 2);
        var stageOneId = mission.Stages.First(s => s.Order == 1).Id;
        var command = new UpdateStageCommand(mission.Id, stageOneId, "Updated", "Description", 2);

        _repository.GetByIdAsync(mission.Id, Arg.Any<CancellationToken>())
            .Returns(mission);
        _lockService.IsMissionInUseAsync(mission.Id, Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*already exists*");
    }
}
