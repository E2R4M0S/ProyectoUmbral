using FluentAssertions;
using Missions.Application.Missions.StatusChange.Chain;
using Missions.Domain.Entities;
using Missions.Domain.Enums;
using Xunit;

namespace Missions.Application.Tests.Missions.StatusChange;

public class DraftToActiveRequiresStagesHandlerTests
{
    [Fact]
    public void Handle_DraftToActive_WithNoStages_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var handler = new DraftToActiveRequiresStagesHandler();
        var mission = Mission.Create("Test", "Test", Difficulty.Easy, 30, MissionType.Treasure);
        // Mission starts as Draft with no stages

        // Act
        var act = () => handler.Handle(mission, "Active");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*No se puede activar una misión sin etapas*");
    }

    [Fact]
    public void Handle_DraftToActive_WithStages_ShouldNotThrow()
    {
        // Arrange
        var handler = new DraftToActiveRequiresStagesHandler();
        var mission = Mission.Create("Test", "Test", Difficulty.Easy, 30, MissionType.Treasure);
        mission.AddStage("Stage 1", "Description", 1);

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
        var handler = new DraftToActiveRequiresStagesHandler();
        var mission = Mission.Create("Test", "Test", Difficulty.Easy, 30, MissionType.Treasure);
        // No stages

        // Act
        var act = () => handler.Handle(mission, targetStatus);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Handle_ActiveToAny_ShouldNotThrow()
    {
        // Arrange
        var handler = new DraftToActiveRequiresStagesHandler();
        var mission = Mission.Create("Test", "Test", Difficulty.Easy, 30, MissionType.Treasure);
        mission.AddStage("Stage 1", "Description", 1);
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
        var handler = new DraftToActiveRequiresStagesHandler();
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