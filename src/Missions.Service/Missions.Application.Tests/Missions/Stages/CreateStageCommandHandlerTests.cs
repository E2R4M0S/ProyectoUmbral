using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Missions.Application.Common.Interfaces;
using Missions.Application.Missions.Stages;
using Missions.Domain.Entities;
using Missions.Domain.Enums;
using Xunit;

namespace Missions.Application.Tests.Missions.Stages;

public class CreateStageCommandHandlerTests
{
    private readonly IMissionRepository _repository = Substitute.For<IMissionRepository>();
    private readonly ILogger<CreateStageCommandHandler> _logger =
        Substitute.For<ILogger<CreateStageCommandHandler>>();
    private readonly CreateStageCommandHandler _sut;

    public CreateStageCommandHandlerTests()
    {
        _sut = new CreateStageCommandHandler(_repository, _logger);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateStageAndReturnResult()
    {
        // Arrange
        var mission = Mission.Create("Test Mission", "Description", Difficulty.Easy, 30, MissionType.Treasure);
        var command = new CreateStageCommand(mission.Id, "Stage One", "First stage description", 1);

        _repository.GetByIdAsync(mission.Id, Arg.Any<CancellationToken>())
            .Returns(mission);
        _repository.UpdateAsync(Arg.Any<Mission>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Stage One");
        result.Description.Should().Be("First stage description");
        result.Order.Should().Be(1);

        await _repository.Received(1).GetByIdAsync(mission.Id, Arg.Any<CancellationToken>());
        await _repository.Received(1).UpdateAsync(Arg.Any<Mission>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenMissionNotFound_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var missionId = Guid.NewGuid();
        var command = new CreateStageCommand(missionId, "Stage One", "First stage description", 1);

        _repository.GetByIdAsync(missionId, Arg.Any<CancellationToken>())
            .Returns((Mission?)null);

        // Act
        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*not found*");

        await _repository.Received(1).GetByIdAsync(missionId, Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Mission>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithDuplicateOrder_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var mission = Mission.Create("Test Mission", "Description", Difficulty.Easy, 30, MissionType.Treasure);
        mission.AddStage("Stage One", "First stage", 1);

        var command = new CreateStageCommand(mission.Id, "Stage One Duplicate", "Duplicate order", 1);

        _repository.GetByIdAsync(mission.Id, Arg.Any<CancellationToken>())
            .Returns(mission);

        // Act
        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");

        await _repository.Received(1).GetByIdAsync(mission.Id, Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Mission>(), Arg.Any<CancellationToken>());
    }
}
