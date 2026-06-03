using FluentAssertions;
using Missions.Domain.Entities;
using Missions.Domain.Enums;
using Xunit;

namespace Missions.Domain.Tests.Entities;

public class MissionCompositeTests
{
    [Fact]
    public void GetLeafCount_2Stages3CluesEach_Returns6()
    {
        // Arrange: 1 mission → 2 stages → 3 clues each
        var mission = Mission.Create("Test Mission", "Description", Difficulty.Easy, 30, MissionType.Treasure);
        mission.AddStage("Stage 1", "First stage", 1);
        mission.AddStage("Stage 2", "Second stage", 2);

        // Add 3 clues to Stage 1
        var stage1Id = mission.Stages[0].Id;
        mission.AddStageClue(stage1Id, "Clue 1", 5, ReleaseType.Auto);
        mission.AddStageClue(stage1Id, "Clue 2", 5, ReleaseType.Auto);
        mission.AddStageClue(stage1Id, "Clue 3", 5, ReleaseType.Auto);

        // Add 3 clues to Stage 2
        var stage2Id = mission.Stages[1].Id;
        mission.AddStageClue(stage2Id, "Clue 4", 5, ReleaseType.Auto);
        mission.AddStageClue(stage2Id, "Clue 5", 5, ReleaseType.Auto);
        mission.AddStageClue(stage2Id, "Clue 6", 5, ReleaseType.Auto);

        // Act
        var leafCount = mission.GetLeafCount();

        // Assert
        leafCount.Should().Be(6);
    }

    [Fact]
    public void GetTotalPenalty_2StagesWithPenalties_Returns30()
    {
        // Arrange: 1 mission → 2 stages with penalties (15 + 15 = 30)
        var mission = Mission.Create("Test Mission", "Description", Difficulty.Medium, 45, MissionType.Treasure);
        mission.AddStage("Stage 1", "First stage", 1);
        mission.AddStage("Stage 2", "Second stage", 2);

        var stage1Id = mission.Stages[0].Id;
        var stage2Id = mission.Stages[1].Id;

        // Stage 1: 3 clues with penalty 5 each = 15
        mission.AddStageClue(stage1Id, "Clue 1", 5, ReleaseType.Auto);
        mission.AddStageClue(stage1Id, "Clue 2", 5, ReleaseType.Auto);
        mission.AddStageClue(stage1Id, "Clue 3", 5, ReleaseType.Auto);

        // Stage 2: 3 clues with penalty 5 each = 15
        mission.AddStageClue(stage2Id, "Clue 4", 5, ReleaseType.Auto);
        mission.AddStageClue(stage2Id, "Clue 5", 5, ReleaseType.Auto);
        mission.AddStageClue(stage2Id, "Clue 6", 5, ReleaseType.Auto);

        // Act
        var totalPenalty = mission.GetTotalPenalty();

        // Assert
        totalPenalty.Should().Be(30);
    }

    [Fact]
    public void GetTotalPenalty_EmptyMission_Returns0()
    {
        // Arrange: mission with no stages
        var mission = Mission.Create("Empty Mission", "No stages", Difficulty.Easy, 30, MissionType.Treasure);

        // Act
        var penalty = mission.GetTotalPenalty();

        // Assert
        penalty.Should().Be(0);
    }

    [Fact]
    public void GetLeafCount_EmptyMission_Returns0()
    {
        // Arrange: mission with no stages
        var mission = Mission.Create("Empty Mission", "No stages", Difficulty.Easy, 30, MissionType.Treasure);

        // Act
        var count = mission.GetLeafCount();

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public void Validate_AllStagesValid_ReturnsValid()
    {
        // Arrange: mission with valid stages and clues
        var mission = Mission.Create("Valid Mission", "Mission description", Difficulty.Easy, 30, MissionType.Treasure);
        mission.AddStage("Stage 1", "First stage", 1);
        var stageId = mission.Stages[0].Id;
        mission.AddStageClue(stageId, "Valid clue", 5, ReleaseType.Auto);

        // Act
        var result = mission.Validate();

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_InvalidClueCascades_ReturnsInvalid()
    {
        // Arrange: mission with deep leaf invalid
        var mission = Mission.Create("Mission with invalid clue", "Description", Difficulty.Easy, 30, MissionType.Treasure);
        mission.AddStage("Stage 1", "First stage", 1);
        var stageId = mission.Stages[0].Id;
        // Add an invalid clue (empty content)
        AddInvalidClueViaReflection(mission, stageId);

        // Act
        var result = mission.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void Validate_EmptyTitle_ReturnsInvalid()
    {
        // Arrange: mission with empty title
        var mission = Mission.Create("", "Description", Difficulty.Easy, 30, MissionType.Treasure);
        mission.AddStage("Stage 1", "First stage", 1);
        var stageId = mission.Stages[0].Id;
        mission.AddStageClue(stageId, "Valid clue", 5, ReleaseType.Auto);

        // Act
        var result = mission.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("title", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_RequiresAtLeastOneStage_ReturnsInvalidWhenEmpty()
    {
        // Arrange: mission with no stages
        var mission = Mission.Create("Mission without stages", "Description", Difficulty.Easy, 30, MissionType.Treasure);

        // Act
        var result = mission.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("stage", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Mission_ImplementsIMissionComponent()
    {
        var mission = Mission.Create("Test Mission", "Description", Difficulty.Easy, 30, MissionType.Treasure);

        mission.Should().BeAssignableTo<IMissionComponent>();
    }

    private static void AddInvalidClueViaReflection(Mission mission, Guid stageId)
    {
        // Use reflection to get the stage and add an invalid clue
        var stagesField = typeof(Mission).GetField("_stages",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var stages = (List<MissionStage>?)stagesField?.GetValue(mission);

        var stage = stages?.FirstOrDefault(s => s.Id == stageId);
        if (stage != null)
        {
            var cluesField = typeof(MissionStage).GetField("_clues",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var clues = (List<MissionClue>?)cluesField?.GetValue(stage);
            clues?.Add(new MissionClue(stageId, "", 5, ReleaseType.Auto));
        }
    }
}