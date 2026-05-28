using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Missions.Application.Common.Interfaces;
using Missions.Application.Missions.Update;
using Missions.Domain.Entities;
using Missions.Domain.Enums;
using Xunit;

namespace Missions.Application.Tests.Missions.Update;

public class UpdateMissionCommandHandlerTests
{
    private readonly IMissionRepository _repository = Substitute.For<IMissionRepository>();
    private readonly IMissionLockService _lockService = Substitute.For<IMissionLockService>();
    private readonly ILogger<UpdateMissionCommandHandler> _logger =
        Substitute.For<ILogger<UpdateMissionCommandHandler>>();
    private readonly UpdateMissionCommandHandler _sut;

    public UpdateMissionCommandHandlerTests()
    {
        _sut = new UpdateMissionCommandHandler(_repository, _lockService, _logger);
    }

    private static Mission CreateMissionWithId(Guid id)
    {
        var mission = Mission.Create(
            "Original Title",
            "Original Description",
            Difficulty.Easy,
            30,
            MissionType.Treasure);
        return mission;
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldUpdateMissionAndSave()
    {
        // Arrange
        var missionId = Guid.NewGuid();
        var command = new UpdateMissionCommand(
            missionId,
            "Updated Title",
            "Updated Description",
            "Hard",
            60);

        var existingMission = CreateMissionWithId(missionId);

        _repository.GetByIdAsync(missionId, Arg.Any<CancellationToken>())
            .Returns(existingMission);
        _lockService.IsMissionInUseAsync(missionId, Arg.Any<CancellationToken>())
            .Returns(false);
        _repository.IsTitleUniqueAsync("Updated Title", Arg.Any<CancellationToken>(), missionId)
            .Returns(true);
        _repository.UpdateAsync(Arg.Any<Mission>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _repository.Received(1).GetByIdAsync(missionId, Arg.Any<CancellationToken>());
        await _repository.Received(1).IsTitleUniqueAsync("Updated Title", Arg.Any<CancellationToken>(), missionId);
        await _repository.Received(1).UpdateAsync(Arg.Any<Mission>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MissionNotFound_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var missionId = Guid.NewGuid();
        var command = new UpdateMissionCommand(
            missionId,
            "Updated Title",
            "Updated Description",
            "Hard",
            60);

        _repository.GetByIdAsync(missionId, Arg.Any<CancellationToken>())
            .Returns((Mission?)null);

        // Act
        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");

        await _repository.Received(1).GetByIdAsync(missionId, Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Mission>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateTitle_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var missionId = Guid.NewGuid();
        var command = new UpdateMissionCommand(
            missionId,
            "Existing Title",
            "Updated Description",
            "Hard",
            60);

        var existingMission = CreateMissionWithId(missionId);

        _repository.GetByIdAsync(missionId, Arg.Any<CancellationToken>())
            .Returns(existingMission);
        _lockService.IsMissionInUseAsync(missionId, Arg.Any<CancellationToken>())
            .Returns(false);
        _repository.IsTitleUniqueAsync("Existing Title", Arg.Any<CancellationToken>(), missionId)
            .Returns(false);

        // Act
        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");

        await _repository.Received(1).IsTitleUniqueAsync("Existing Title", Arg.Any<CancellationToken>(), missionId);
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Mission>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_LockedMission_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var missionId = Guid.NewGuid();
        var command = new UpdateMissionCommand(
            missionId,
            "Updated Title",
            "Updated Description",
            "Hard",
            60);

        var existingMission = CreateMissionWithId(missionId);

        _repository.GetByIdAsync(missionId, Arg.Any<CancellationToken>())
            .Returns(existingMission);
        _lockService.IsMissionInUseAsync(missionId, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*is in use*");

        await _lockService.Received(1).IsMissionInUseAsync(missionId, Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Mission>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCallUpdateAsyncWithCorrectEntity()
    {
        // Arrange
        var missionId = Guid.NewGuid();
        var command = new UpdateMissionCommand(
            missionId,
            "Updated Title",
            "Updated Description",
            "Hard",
            60);

        var existingMission = CreateMissionWithId(missionId);

        _repository.GetByIdAsync(missionId, Arg.Any<CancellationToken>())
            .Returns(existingMission);
        _lockService.IsMissionInUseAsync(missionId, Arg.Any<CancellationToken>())
            .Returns(false);
        _repository.IsTitleUniqueAsync("Updated Title", Arg.Any<CancellationToken>(), missionId)
            .Returns(true);
        _repository.UpdateAsync(Arg.Any<Mission>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _repository.Received(1).UpdateAsync(Arg.Do<Mission>(m =>
        {
            m.Title.Should().Be("Updated Title");
            m.Description.Should().Be("Updated Description");
            m.Difficulty.Should().Be(Difficulty.Hard);
            m.TimeMinutes.Should().Be(60);
        }), Arg.Any<CancellationToken>());
    }
}
