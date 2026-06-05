using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Missions.Application.Common.Interfaces;
using Missions.Application.Common.Services;
using Missions.Domain.Entities;
using Missions.Domain.Enums;
using Xunit;

namespace Missions.Application.Tests.Common.Services;

public class MissionStageValidatorTests
{
    private readonly IMissionRepository _repository = Substitute.For<IMissionRepository>();
    private readonly MissionStageValidator _sut;

    public MissionStageValidatorTests()
    {
        _sut = new MissionStageValidator(_repository);
    }

    [Fact]
    public async Task HasAtLeastOneStageAsync_WithStages_ShouldReturnTrue()
    {
        // Arrange
        var mission = Mission.Create("Test", "Test", Difficulty.Easy, 30, MissionType.Treasure);
        mission.AddStage("Stage 1", "Description", 1);

        _repository.GetByIdAsync(mission.Id, Arg.Any<CancellationToken>())
            .Returns(mission);

        // Act
        var result = await _sut.HasAtLeastOneStageAsync(mission.Id, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasAtLeastOneStageAsync_WithNoStages_ShouldReturnFalse()
    {
        // Arrange
        var mission = Mission.Create("Test", "Test", Difficulty.Easy, 30, MissionType.Treasure);
        // No stages added

        _repository.GetByIdAsync(mission.Id, Arg.Any<CancellationToken>())
            .Returns(mission);

        // Act
        var result = await _sut.HasAtLeastOneStageAsync(mission.Id, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasAtLeastOneStageAsync_WhenMissionNotFound_ShouldReturnFalse()
    {
        // Arrange
        var missionId = Guid.NewGuid();

        _repository.GetByIdAsync(missionId, Arg.Any<CancellationToken>())
            .Returns((Mission?)null);

        // Act
        var result = await _sut.HasAtLeastOneStageAsync(missionId, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasAtLeastOneStageAsync_ShouldCallRepositoryGetByIdAsync()
    {
        // Arrange
        var missionId = Guid.NewGuid();

        _repository.GetByIdAsync(missionId, Arg.Any<CancellationToken>())
            .Returns((Mission?)null);

        // Act
        await _sut.HasAtLeastOneStageAsync(missionId, CancellationToken.None);

        // Assert
        await _repository.Received(1).GetByIdAsync(missionId, Arg.Any<CancellationToken>());
    }
}