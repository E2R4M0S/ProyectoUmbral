using FluentAssertions;
using Missions.Domain.Entities;
using Missions.Domain.Enums;
using Xunit;

namespace Missions.Domain.Tests;

public class MissionStageTests
{
    [Fact]
    public void Validate_NoClues_ShouldFail()
    {
        var mission = Mission.Create("T", "D", Difficulty.Easy, 30, MissionType.Treasure);
        mission.AddStage("S1", "Desc", 1);
        var result = mission.Stages[0].Validate();
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateStage_ShouldChangeProperties()
    {
        var mission = Mission.Create("T", "D", Difficulty.Easy, 30, MissionType.Treasure);
        mission.AddStage("Old", "Old D", 1);
        var stageId = mission.Stages[0].Id;
        mission.UpdateStage(stageId, "New", "New D", 2);
        mission.Stages[0].Name.Should().Be("New");
        mission.Stages[0].Order.Should().Be(2);
    }

    [Fact]
    public void GetTotalPenalty_ShouldSumCluePenalties()
    {
        var mission = Mission.Create("T", "D", Difficulty.Easy, 30, MissionType.Treasure);
        mission.AddStage("S1", "Desc", 1);
        var s = mission.Stages[0];
        mission.AddStageClue(s.Id, "C1", 5, ReleaseType.Manual);
        mission.AddStageClue(s.Id, "C2", 10, ReleaseType.Manual);
        s.GetTotalPenalty().Should().Be(15);
    }

    [Fact]
    public void GetLeafCount_ShouldCountClues()
    {
        var mission = Mission.Create("T", "D", Difficulty.Easy, 30, MissionType.Treasure);
        mission.AddStage("S1", "Desc", 1);
        mission.AddStageClue(mission.Stages[0].Id, "C1", null, ReleaseType.Manual);
        mission.AddStageClue(mission.Stages[0].Id, "C2", null, ReleaseType.Manual);
        mission.Stages[0].GetLeafCount().Should().Be(2);
    }
}
