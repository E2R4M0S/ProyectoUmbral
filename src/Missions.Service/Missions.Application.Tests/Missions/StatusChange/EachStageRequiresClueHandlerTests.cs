using FluentAssertions;
using Missions.Application.Missions.StatusChange.Chain;
using Missions.Domain.Entities;
using Missions.Domain.Enums;
using Xunit;

namespace Missions.Application.Tests.Missions.StatusChange;

public class EachStageRequiresClueHandlerTests
{
    [Fact]
    public void Handle_DraftToActive_WithStageWithoutClues_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var handler = new EachStageRequiresClueHandler();
        var mission = Mission.Create("Test", "Test", Difficulty.Easy, 30, MissionType.Treasure);
        mission.AddStage("Stage 1", "Description", 1);
        // Stage has no clues

        // Act
        var act = () => handler.Handle(mission, "Active");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*etapas sin pistas*");
    }

    [Fact]
    public void Handle_DraftToActive_WithAllStagesHavingClues_ShouldNotThrow()
    {
        // Arrange
        var handler = new EachStageRequiresClueHandler();
        var mission = Mission.Create("Test", "Test", Difficulty.Easy, 30, MissionType.Treasure);
        mission.AddStage("Stage 1", "Description", 1);
        mission.AddStageClue(mission.Stages[0].Id, "Clue content", null, ReleaseType.Manual);

        // Act
        var act = () => handler.Handle(mission, "Active");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Handle_DraftToActive_WithNoStagesAtAll_ShouldNotThrow()
    {
        // Arrange: this handler only cares about clues, not stage count (that's the previous handler's job)
        var handler = new EachStageRequiresClueHandler();
        var mission = Mission.Create("Test", "Test", Difficulty.Easy, 30, MissionType.Treasure);

        // Act
        var act = () => handler.Handle(mission, "Active");

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("Inactive")]
    [InlineData("Draft")]
    public void Handle_DraftToNonActive_ShouldNotThrow(string targetStatus)
    {
        // Arrange
        var handler = new EachStageRequiresClueHandler();
        var mission = Mission.Create("Test", "Test", Difficulty.Easy, 30, MissionType.Treasure);
        mission.AddStage("Stage 1", "Description", 1);
        // No clues, but we're not activating

        // Act
        var act = () => handler.Handle(mission, targetStatus);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Handle_ActiveToAny_ShouldNotThrow()
    {
        // Arrange
        var handler = new EachStageRequiresClueHandler();
        var mission = Mission.Create("Test", "Test", Difficulty.Easy, 30, MissionType.Treasure);
        mission.AddStage("Stage 1", "Description", 1);
        mission.AddStageClue(mission.Stages[0].Id, "Clue content", null, ReleaseType.Manual);
        mission.SetStatus(MissionStatus.Active);

        // Act
        var act = () => handler.Handle(mission, "Inactive");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void SetNext_ShouldReturnHandlerForChaining()
    {
        // Arrange
        var handler = new EachStageRequiresClueHandler();
        var nextHandler = new MockNextHandler();

        // Act
        var result = handler.SetNext(nextHandler);

        // Assert
        result.Should().Be(nextHandler);
    }

    private class MockNextHandler : IMissionStatusHandler
    {
        public IMissionStatusHandler SetNext(IMissionStatusHandler handler) => handler;
        public void Handle(Mission mission, string newStatus) { }
    }
}
