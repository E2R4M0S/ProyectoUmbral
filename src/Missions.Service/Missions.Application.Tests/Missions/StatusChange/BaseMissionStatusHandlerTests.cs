using FluentAssertions;
using Missions.Application.Missions.StatusChange.Chain;
using Missions.Domain.Entities;
using Missions.Domain.Enums;
using Xunit;

namespace Missions.Application.Tests.Missions.StatusChange;

public class BaseMissionStatusHandlerTests
{
    [Fact]
    public void Handle_WithNoNextHandler_ShouldNotThrow()
    {
        // Arrange
        var handler = new ConcreteStatusHandler(() => { }); // Does nothing
        var mission = Mission.Create("Test", "Test", Difficulty.Easy, 30, MissionType.Treasure);

        // Act
        var act = () => handler.Handle(mission, "Active");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Handle_WithNextHandler_ShouldCallNext()
    {
        // Arrange
        var callSequence = new List<string>();
        var handler = new ConcreteStatusHandler(() => callSequence.Add("first"));
        var nextHandler = new ConcreteStatusHandler(() => callSequence.Add("second"));
        handler.SetNext(nextHandler);
        var mission = Mission.Create("Test", "Test", Difficulty.Easy, 30, MissionType.Treasure);

        // Act
        handler.Handle(mission, "Active");

        // Assert
        callSequence.Should().ContainInOrder("first", "second");
    }

    [Fact]
    public void Handle_WhenValidateThrows_ShouldNotCallNext()
    {
        // Arrange
        var callSequence = new List<string>();
        var handler = new ConcreteStatusHandler(() => throw new InvalidOperationException("validation failed"));
        var nextHandler = new ConcreteStatusHandler(() => callSequence.Add("should not be called"));
        handler.SetNext(nextHandler);
        var mission = Mission.Create("Test", "Test", Difficulty.Easy, 30, MissionType.Treasure);

        // Act
        var act = () => handler.Handle(mission, "Active");

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("validation failed");
        callSequence.Should().NotContain("should not be called");
    }

    [Fact]
    public void SetNext_ShouldReturnHandlerForChaining()
    {
        // Arrange
        var handler = new ConcreteStatusHandler(() => { });
        var nextHandler = new ConcreteStatusHandler(() => { });

        // Act
        var result = handler.SetNext(nextHandler);

        // Assert
        result.Should().Be(nextHandler);
    }

    [Fact]
    public void Handle_MultipleHandlersInChain_ShouldExecuteInOrder()
    {
        // Arrange
        var callSequence = new List<int>();
        var first = new NumberedHandler(1, () => { }, callSequence);
        var second = new NumberedHandler(2, () => { }, callSequence);
        var third = new NumberedHandler(3, () => { }, callSequence);

        first.SetNext(second).SetNext(third);
        var mission = Mission.Create("Test", "Test", Difficulty.Easy, 30, MissionType.Treasure);

        // Act
        first.Handle(mission, "Active");

        // Assert
        callSequence.Should().ContainInOrder(1, 2, 3);
        callSequence.Should().HaveCount(3);
    }

    private class ConcreteStatusHandler : BaseMissionStatusHandler
    {
        private readonly Action _validateAction;

        public ConcreteStatusHandler(Action validateAction)
        {
            _validateAction = validateAction;
        }

        protected override void Validate(Mission mission, string newStatus)
        {
            _validateAction();
        }
    }

    private class NumberedHandler : BaseMissionStatusHandler
    {
        private readonly int _number;
        private readonly Action _validateAction;
        private readonly List<int> _callSequence;

        public NumberedHandler(int number, Action validateAction, List<int> callSequence)
        {
            _number = number;
            _validateAction = validateAction;
            _callSequence = callSequence;
        }

        protected override void Validate(Mission mission, string newStatus)
        {
            _validateAction();
            _callSequence.Add(_number);
        }
    }
}