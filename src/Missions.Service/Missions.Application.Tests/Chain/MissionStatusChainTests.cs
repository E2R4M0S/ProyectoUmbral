using FluentAssertions;
using Missions.Application.Missions.StatusChange.Chain;
using Missions.Domain.Entities;
using Missions.Domain.Enums;
using Xunit;

namespace Missions.Application.Tests.Chain;

public class ValidMissionStatusHandlerTests
{
    [Fact]
    public void ValidStatus_ShouldPass()
    {
        var handler = new ValidMissionStatusHandler();
        var mission = Mission.Create("Test", "Desc", Difficulty.Easy, 30, MissionType.Treasure);
        var act = () => handler.Handle(mission, "Active");
        act.Should().NotThrow();
    }

    [Fact]
    public void InvalidStatus_ShouldThrow()
    {
        var handler = new ValidMissionStatusHandler();
        var mission = Mission.Create("Test", "Desc", Difficulty.Easy, 30, MissionType.Treasure);
        var act = () => handler.Handle(mission, "Invalid");
        act.Should().Throw<InvalidOperationException>();
    }
}

public class DraftToActiveRequiresStagesHandlerTests
{
    [Fact]
    public void DraftWithStages_ShouldPass()
    {
        var handler = new DraftToActiveRequiresStagesHandler();
        var mission = Mission.Create("Test", "Desc", Difficulty.Easy, 30, MissionType.Treasure);
        mission.AddStage("Stage 1", "Desc", 1);
        var act = () => handler.Handle(mission, "Active");
        act.Should().NotThrow();
    }

    [Fact]
    public void DraftWithoutStages_ShouldThrow()
    {
        var handler = new DraftToActiveRequiresStagesHandler();
        var mission = Mission.Create("Test", "Desc", Difficulty.Easy, 30, MissionType.Treasure);
        var act = () => handler.Handle(mission, "Active");
        act.Should().Throw<InvalidOperationException>();
    }
}
