using FluentAssertions;
using Missions.Domain.Entities;
using Missions.Domain.Enums;
using Xunit;

namespace Missions.Application.Tests.Missions.Stages;

public class UpdateStageCommandDomainTests
{
    [Fact]
    public void Mission_UpdateStage_WithValidData_ShouldUpdateStageProperties()
    {
        // Arrange
        var mission = Mission.Create("Test Mission", "Description", Difficulty.Easy, 30, MissionType.Treasure);
        mission.AddStage("Original Name", "Original Description", 1);
        var stageId = mission.Stages.Single().Id;

        // Act
        mission.UpdateStage(stageId, "Updated Name", "Updated Description", 2);
        var updatedStage = mission.Stages.Single();

        // Assert
        updatedStage.Name.Should().Be("Updated Name");
        updatedStage.Description.Should().Be("Updated Description");
        updatedStage.Order.Should().Be(2);
    }

    [Fact]
    public void Mission_UpdateStage_WithWhitespaceInName_ShouldTrim()
    {
        // Arrange
        var mission = Mission.Create("Test Mission", "Description", Difficulty.Easy, 30, MissionType.Treasure);
        mission.AddStage("Original", "Description", 1);
        var stageId = mission.Stages.Single().Id;

        // Act
        mission.UpdateStage(stageId, "  Trimmed Name  ", "Description", 1);
        var updatedStage = mission.Stages.Single();

        // Assert
        updatedStage.Name.Should().Be("Trimmed Name");
    }

    [Fact]
    public void Mission_UpdateStage_WhenStageNotFound_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var mission = Mission.Create("Test Mission", "Description", Difficulty.Easy, 30, MissionType.Treasure);
        mission.AddStage("Stage One", "First stage", 1);
        var wrongStageId = Guid.NewGuid();

        // Act
        Action act = () => mission.UpdateStage(wrongStageId, "Updated", "Description", 2);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*not found*");
    }

    [Fact]
    public void Mission_UpdateStage_WithDuplicateOrder_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var mission = Mission.Create("Test Mission", "Description", Difficulty.Easy, 30, MissionType.Treasure);
        mission.AddStage("Stage One", "First stage", 1);
        mission.AddStage("Stage Two", "Second stage", 2);
        var stageOneId = mission.Stages.First(s => s.Order == 1).Id;

        // Act
        Action act = () => mission.UpdateStage(stageOneId, "Updated", "Description", 2);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*already exists*");
    }

    [Fact]
    public void Mission_UpdateStage_KeepingSameOrder_ShouldSucceed()
    {
        // Arrange
        var mission = Mission.Create("Test Mission", "Description", Difficulty.Easy, 30, MissionType.Treasure);
        mission.AddStage("Stage One", "First stage", 1);
        mission.AddStage("Stage Two", "Second stage", 2);
        var stageOneId = mission.Stages.First(s => s.Order == 1).Id;

        // Act - updating stage 1 to same order should work
        mission.UpdateStage(stageOneId, "Updated Name", "Updated Description", 1);
        var updatedStage = mission.Stages.First(s => s.Order == 1);

        // Assert
        updatedStage.Name.Should().Be("Updated Name");
        updatedStage.Description.Should().Be("Updated Description");
    }
}
