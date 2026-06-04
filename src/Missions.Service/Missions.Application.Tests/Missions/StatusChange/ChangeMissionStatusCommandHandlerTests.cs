using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Missions.Application.Common.Interfaces;
using Missions.Application.Missions.StatusChange;
using Missions.Domain.Entities;
using Missions.Domain.Enums;
using Xunit;

namespace Missions.Application.Tests.Missions.StatusChange;

public class ChangeMissionStatusCommandHandlerTests
{
    private readonly IMissionRepository _repository = Substitute.For<IMissionRepository>();
    private readonly IMissionLockService _lockService = Substitute.For<IMissionLockService>();
    private readonly IMissionStageValidator _stageValidator = Substitute.For<IMissionStageValidator>();
    private readonly ILogger<ChangeMissionStatusCommandHandler> _logger =
        Substitute.For<ILogger<ChangeMissionStatusCommandHandler>>();
    private readonly ChangeMissionStatusCommandHandler _sut;

    public ChangeMissionStatusCommandHandlerTests()
    {
        _sut = new ChangeMissionStatusCommandHandler(
            _repository, _lockService, _stageValidator, _logger);
    }

    private static Mission CreateMissionWithId(Guid id, MissionStatus status)
    {
        var mission = Mission.Create(
            "Test Mission",
            "Test Description",
            Difficulty.Easy,
            30,
            MissionType.Treasure);
        // Use reflection or create differently to set status... 
        // Actually, Mission.Create sets status to Draft. We need to test various statuses.
        // Let's test Draft->Active, Active->Inactive, etc.
        // For testing, we'll create a mission and test transitions directly
        return mission;
    }

    [Fact]
    public async Task Handle_DraftToActive_ShouldSucceed()
    {
        // Arrange
        var missionId = Guid.NewGuid();
        var command = new ChangeMissionStatusCommand(missionId, "Active");

        var existingMission = Mission.Create(
            "Test", "Test", Difficulty.Easy, 30, MissionType.Treasure);

        _repository.GetByIdAsync(missionId, Arg.Any<CancellationToken>())
            .Returns(existingMission);
        _stageValidator.HasAtLeastOneStageAsync(missionId, Arg.Any<CancellationToken>())
            .Returns(true);
        _lockService.IsMissionInUseAsync(missionId, Arg.Any<CancellationToken>())
            .Returns(false);
        _repository.UpdateAsync(Arg.Any<Mission>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _repository.Received(1).UpdateAsync(Arg.Any<Mission>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SameStatus_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var missionId = Guid.NewGuid();
        var command = new ChangeMissionStatusCommand(missionId, "Active");

        var existingMission = Mission.Create(
            "Test", "Test", Difficulty.Easy, 30, MissionType.Treasure);
        // Mission starts as Draft, transition to Active first
        existingMission.SetStatus(MissionStatus.Active);

        _repository.GetByIdAsync(missionId, Arg.Any<CancellationToken>())
            .Returns(existingMission);
        _stageValidator.HasAtLeastOneStageAsync(missionId, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already*");
    }

    [Fact]
    public async Task Handle_MissionNotFound_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var missionId = Guid.NewGuid();
        var command = new ChangeMissionStatusCommand(missionId, "Active");

        _repository.GetByIdAsync(missionId, Arg.Any<CancellationToken>())
            .Returns((Mission?)null);

        // Act
        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");

        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Mission>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ActiveToInactive_ShouldSucceed()
    {
        // Arrange
        var missionId = Guid.NewGuid();
        var command = new ChangeMissionStatusCommand(missionId, "Inactive");

        var mission = Mission.Create("Test", "Test", Difficulty.Easy, 30, MissionType.Treasure);
        mission.SetStatus(MissionStatus.Active);

        _repository.GetByIdAsync(missionId, Arg.Any<CancellationToken>())
            .Returns(mission);
        _lockService.IsMissionInUseAsync(missionId, Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        mission.Status.Should().Be(MissionStatus.Inactive);
        await _repository.Received(1).UpdateAsync(mission, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InactiveToActive_ShouldSucceed()
    {
        // Arrange
        var missionId = Guid.NewGuid();
        var command = new ChangeMissionStatusCommand(missionId, "Active");

        var mission = Mission.Create("Test", "Test", Difficulty.Easy, 30, MissionType.Treasure);
        mission.SetStatus(MissionStatus.Active);
        mission.SetStatus(MissionStatus.Inactive);

        _repository.GetByIdAsync(missionId, Arg.Any<CancellationToken>())
            .Returns(mission);
        _stageValidator.HasAtLeastOneStageAsync(missionId, Arg.Any<CancellationToken>())
            .Returns(true);
        _lockService.IsMissionInUseAsync(missionId, Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        mission.Status.Should().Be(MissionStatus.Active);
        await _repository.Received(1).UpdateAsync(mission, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DraftToInactive_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var missionId = Guid.NewGuid();
        var command = new ChangeMissionStatusCommand(missionId, "Inactive");

        var mission = Mission.Create("Test", "Test", Difficulty.Easy, 30, MissionType.Treasure);

        _repository.GetByIdAsync(missionId, Arg.Any<CancellationToken>())
            .Returns(mission);
        _lockService.IsMissionInUseAsync(missionId, Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cannot transition*");
    }

    [Fact]
    public async Task Handle_LockedMission_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var missionId = Guid.NewGuid();
        // Transition to Inactive when mission is already Active
        var command = new ChangeMissionStatusCommand(missionId, "Inactive");

        var existingMission = Mission.Create(
            "Test", "Test", Difficulty.Easy, 30, MissionType.Treasure);
        // Mission starts as Draft, transition to Active first
        existingMission.SetStatus(MissionStatus.Active);

        _repository.GetByIdAsync(missionId, Arg.Any<CancellationToken>())
            .Returns(existingMission);
        _stageValidator.HasAtLeastOneStageAsync(missionId, Arg.Any<CancellationToken>())
            .Returns(true);
        _lockService.IsMissionInUseAsync(missionId, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*is in use*");
    }
}