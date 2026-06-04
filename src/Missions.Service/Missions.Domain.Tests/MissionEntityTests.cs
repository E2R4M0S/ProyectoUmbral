using FluentAssertions;
using Missions.Domain.Entities;
using Missions.Domain.Enums;
using Xunit;

namespace Missions.Domain.Tests;

public class MissionEntityTests
{
    [Fact]
    public void Create_ShouldSetDraftStatus()
    {
        var mission = Mission.Create("Test", "Desc", Difficulty.Easy, 30, MissionType.Treasure);
        mission.Status.Should().Be(MissionStatus.Draft);
        mission.Title.Should().Be("Test");
    }

    [Fact]
    public void Update_ShouldChangeProperties()
    {
        var mission = Mission.Create("Old", "Old Desc", Difficulty.Easy, 30, MissionType.Treasure);
        mission.Update("New", "New Desc", Difficulty.Hard, 60);
        mission.Title.Should().Be("New");
        mission.Difficulty.Should().Be(Difficulty.Hard);
        mission.TimeMinutes.Should().Be(60);
    }

    [Fact]
    public void SetStatus_ValidTransition_ShouldSucceed()
    {
        var mission = Mission.Create("Test", "Desc", Difficulty.Easy, 30, MissionType.Treasure);
        mission.AddStage("S1", "Desc", 1);
        mission.SetStatus(MissionStatus.Active);
        mission.Status.Should().Be(MissionStatus.Active);
    }

    [Fact]
    public void AddStageClue_ShouldAddClueToStage()
    {
        var mission = Mission.Create("Test", "Desc", Difficulty.Easy, 30, MissionType.Treasure);
        mission.AddStage("S1", "Desc", 1);
        var stage = mission.Stages[0];
        mission.AddStageClue(stage.Id, "Pista secreta", 5, ReleaseType.Manual);
        stage.Clues.Should().HaveCount(1);
        stage.Clues[0].Content.Should().Be("Pista secreta");
    }

    [Fact]
    public void RemoveStageClue_ShouldRemove()
    {
        var mission = Mission.Create("Test", "Desc", Difficulty.Easy, 30, MissionType.Treasure);
        mission.AddStage("S1", "Desc", 1);
        var stage = mission.Stages[0];
        mission.AddStageClue(stage.Id, "Pista", null, ReleaseType.Auto);
        var clueId = stage.Clues[0].Id;
        mission.RemoveStageClue(stage.Id, clueId);
        stage.Clues.Should().BeEmpty();
    }

    [Fact]
    public void GetTotalPenalty_ShouldSumAllClues()
    {
        var mission = Mission.Create("Test", "Desc", Difficulty.Easy, 30, MissionType.Treasure);
        mission.AddStage("S1", "Desc", 1);
        var s1 = mission.Stages[0];
        mission.AddStageClue(s1.Id, "C1", 5, ReleaseType.Manual);
        mission.AddStageClue(s1.Id, "C2", 10, ReleaseType.Manual);
        mission.GetTotalPenalty().Should().Be(15);
    }

    [Fact]
    public void GetLeafCount_ShouldCountAllClues()
    {
        var mission = Mission.Create("Test", "Desc", Difficulty.Easy, 30, MissionType.Treasure);
        mission.AddStage("S1", "Desc", 1);
        mission.AddStageClue(mission.Stages[0].Id, "C1", null, ReleaseType.Manual);
        mission.AddStageClue(mission.Stages[0].Id, "C2", null, ReleaseType.Manual);
        mission.GetLeafCount().Should().Be(2);
    }

    [Fact]
    public void Validate_DraftWithoutStages_ShouldFail()
    {
        var mission = Mission.Create("Test", "Desc", Difficulty.Easy, 30, MissionType.Treasure);
        var result = mission.Validate();
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithStages_ShouldPass()
    {
        var mission = Mission.Create("Test", "Desc", Difficulty.Easy, 30, MissionType.Treasure);
        mission.AddStage("S1", "Desc", 1);
        mission.AddStageClue(mission.Stages[0].Id, "Pista", null, ReleaseType.Manual);
        var result = mission.Validate();
        result.IsValid.Should().BeTrue();
    }
}
