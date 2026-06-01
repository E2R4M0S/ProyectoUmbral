using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Missions.Application.Common.Interfaces;
using Missions.Application.Missions.Clues;
using Missions.Domain.Entities;
using Missions.Domain.Enums;
using Xunit;

namespace Missions.Application.Tests.Missions.Clues;

public class CreateClueCommandHandlerTests
{
    private readonly IMissionRepository _repository = Substitute.For<IMissionRepository>();
    private readonly ILogger<CreateClueCommandHandler> _logger =
        Substitute.For<ILogger<CreateClueCommandHandler>>();
    private readonly CreateClueCommandHandler _sut;

    public CreateClueCommandHandlerTests()
    {
        _sut = new CreateClueCommandHandler(_repository, _logger);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateClueAndReturnResult()
    {
        // Arrange
        var mission = Mission.Create("Test Mission", "Description", Difficulty.Easy, 30, MissionType.Treasure);
        mission.AddStage("Stage One", "First stage", 1);
        var stageId = mission.Stages.Single().Id;
        var command = new CreateClueCommand(mission.Id, stageId, "Clue content here", null, "Auto");

        _repository.GetByIdAsync(mission.Id, Arg.Any<CancellationToken>())
            .Returns(mission);
        _repository.AddClueAsync(Arg.Any<Mission>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().Be("Clue content here");
        result.Penalty.Should().BeNull();
        result.ReleaseType.Should().Be("Auto");

        await _repository.Received(1).GetByIdAsync(mission.Id, Arg.Any<CancellationToken>());
        await _repository.Received(1).AddClueAsync(Arg.Any<Mission>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenMissionNotFound_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var missionId = Guid.NewGuid();
        var stageId = Guid.NewGuid();
        var command = new CreateClueCommand(missionId, stageId, "Clue content", null, "Auto");

        _repository.GetByIdAsync(missionId, Arg.Any<CancellationToken>())
            .Returns((Mission?)null);

        // Act
        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*not found*");

        await _repository.Received(1).GetByIdAsync(missionId, Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().AddClueAsync(Arg.Any<Mission>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithPenalty_ShouldCreateClueWithPenalty()
    {
        // Arrange
        var mission = Mission.Create("Test Mission", "Description", Difficulty.Easy, 30, MissionType.Treasure);
        mission.AddStage("Stage One", "First stage", 1);
        var stageId = mission.Stages.Single().Id;
        var command = new CreateClueCommand(mission.Id, stageId, "Hard clue", 30, "Manual");

        _repository.GetByIdAsync(mission.Id, Arg.Any<CancellationToken>())
            .Returns(mission);
        _repository.AddClueAsync(Arg.Any<Mission>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Penalty.Should().Be(30);
        result.ReleaseType.Should().Be("Manual");
    }
}
