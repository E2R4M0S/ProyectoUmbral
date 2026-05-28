using FluentAssertions;
using Missions.Domain.Entities;
using Missions.Domain.Enums;
using Xunit;

namespace Missions.Application.Tests.Missions.Clues;

public class MissionClueDomainTests
{
    [Fact]
    public void Mission_AddStageClue_WithValidData_ShouldAddClueToStage()
    {
        // Arrange
        var mission = Mission.Create("Test Mission", "Description", Difficulty.Easy, 30, MissionType.Treasure);
        mission.AddStage("Stage One", "First stage", 1);
        var stageId = mission.Stages.Single().Id;

        // Act
        mission.AddStageClue(stageId, "Clue content here", null, ReleaseType.Auto);
        var stage = mission.Stages.Single();

        // Assert
        stage.Clues.Should().HaveCount(1);
        stage.Clues.Single().Content.Should().Be("Clue content here");
        stage.Clues.Single().Penalty.Should().BeNull();
        stage.Clues.Single().ReleaseType.Should().Be(ReleaseType.Auto);
    }

    [Fact]
    public void Mission_AddStageClue_WithPenalty_ShouldAddClueWithPenalty()
    {
        // Arrange
        var mission = Mission.Create("Test Mission", "Description", Difficulty.Easy, 30, MissionType.Treasure);
        mission.AddStage("Stage One", "First stage", 1);
        var stageId = mission.Stages.Single().Id;

        // Act
        mission.AddStageClue(stageId, "Hard clue", 30, ReleaseType.Manual);
        var clue = mission.Stages.Single().Clues.Single();

        // Assert
        clue.Content.Should().Be("Hard clue");
        clue.Penalty.Should().Be(30);
        clue.ReleaseType.Should().Be(ReleaseType.Manual);
    }

    [Fact]
    public void Mission_AddStageClue_WhenStageNotFound_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var mission = Mission.Create("Test Mission", "Description", Difficulty.Easy, 30, MissionType.Treasure);
        mission.AddStage("Stage One", "First stage", 1);
        var wrongStageId = Guid.NewGuid();

        // Act
        Action act = () => mission.AddStageClue(wrongStageId, "Clue content", null, ReleaseType.Auto);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*Stage with id '{wrongStageId}' not found*");
    }

    [Fact]
    public void Mission_RemoveStageClue_WithValidClueId_ShouldRemoveClue()
    {
        var mission = Mission.Create("Test Mission", "Description", Difficulty.Easy, 30, MissionType.Treasure);
        mission.AddStage("Stage One", "First stage", 1);
        var stageId = mission.Stages.Single().Id;
        mission.AddStageClue(stageId, "Clue to remove", null, ReleaseType.Auto);
        var clueId = mission.Stages.Single().Clues.Single().Id;

        mission.RemoveStageClue(stageId, clueId);

        mission.Stages.Single().Clues.Should().BeEmpty();
    }

    [Fact]
    public void Mission_RemoveStageClue_WhenClueNotFound_ShouldThrowInvalidOperationException()
    {
        var mission = Mission.Create("Test Mission", "Description", Difficulty.Easy, 30, MissionType.Treasure);
        mission.AddStage("Stage One", "First stage", 1);
        var stageId = mission.Stages.Single().Id;
        var wrongClueId = Guid.NewGuid();

        Action act = () => mission.RemoveStageClue(stageId, wrongClueId);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*Clue with id '{wrongClueId}' not found*");
    }

    [Fact]
    public void Mission_RemoveStageClue_WhenStageNotFound_ShouldThrowInvalidOperationException()
    {
        var mission = Mission.Create("Test Mission", "Description", Difficulty.Easy, 30, MissionType.Treasure);
        mission.AddStage("Stage One", "First stage", 1);
        mission.AddStageClue(mission.Stages.Single().Id, "Some clue", null, ReleaseType.Auto);
        var wrongStageId = Guid.NewGuid();
        var clueId = mission.Stages.Single().Clues.Single().Id;

        Action act = () => mission.RemoveStageClue(wrongStageId, clueId);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*Stage with id '{wrongStageId}' not found*");
    }

}
