using FluentAssertions;
using Missions.Application.Missions.StatusChange.Chain;
using Missions.Domain.Entities;
using Missions.Domain.Enums;
using Xunit;

namespace Missions.Application.Tests.Missions.StatusChange;

public class ValidMissionStatusHandlerTests
{
    [Fact]
    public void Handle_WithValidStatus_ShouldNotThrow()
    {
        // Arrange
        var handler = new ValidMissionStatusHandler();
        var mission = Mission.Create("Test", "Test", Difficulty.Easy, 30, MissionType.Treasure);

        // Act
        var act = () => handler.Handle(mission, "Active");

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("Active")]
    [InlineData("Draft")]
    [InlineData("Inactive")]
    public void Handle_WithAllValidStatuses_ShouldNotThrow(string status)
    {
        // Arrange
        var handler = new ValidMissionStatusHandler();
        var mission = Mission.Create("Test", "Test", Difficulty.Easy, 30, MissionType.Treasure);

        // Act
        var act = () => handler.Handle(mission, status);

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("InvalidStatus")]
    [InlineData("")]
    [InlineData("PENDING")]
    [InlineData("archived")]
    public void Handle_WithInvalidStatus_ShouldThrowInvalidOperationException(string invalidStatus)
    {
        // Arrange
        var handler = new ValidMissionStatusHandler();
        var mission = Mission.Create("Test", "Test", Difficulty.Easy, 30, MissionType.Treasure);

        // Act
        var act = () => handler.Handle(mission, invalidStatus);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*'{invalidStatus}' no es un estado válido*");
    }

    [Fact]
    public void Handle_ShouldBeCaseInsensitive()
    {
        // Arrange
        var handler = new ValidMissionStatusHandler();
        var mission = Mission.Create("Test", "Test", Difficulty.Easy, 30, MissionType.Treasure);

        // Act
        var act = () => handler.Handle(mission, "active");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void SetNext_ShouldReturnHandlerForChaining()
    {
        // Arrange
        var handler = new ValidMissionStatusHandler();
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