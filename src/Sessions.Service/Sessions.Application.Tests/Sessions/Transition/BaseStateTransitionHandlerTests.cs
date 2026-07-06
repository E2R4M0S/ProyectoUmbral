using FluentAssertions;
using Sessions.Application.Sessions.Transition.Chain;
using Sessions.Domain.Entities;
using Sessions.Domain.Enums;
using Xunit;

namespace Sessions.Application.Tests.Sessions.Transition;

public class BaseStateTransitionHandlerTests
{
    [Fact]
    public void Handle_WithNoNextHandler_ShouldNotThrow()
    {
        // Arrange
        var handler = new ConcreteStateHandler(() => { }); // Does nothing
        var session = Session.Create("Test Session", "123456", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), "Test Mission", "Trivia", 1) });

        // Act
        var act = () => handler.Handle(session, "Active");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Handle_WithNextHandler_ShouldCallNext()
    {
        // Arrange
        var callSequence = new List<string>();
        var handler = new ConcreteStateHandler(() => callSequence.Add("first"));
        var nextHandler = new ConcreteStateHandler(() => callSequence.Add("second"));
        handler.SetNext(nextHandler);
        var session = Session.Create("Test Session", "123456", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), "Test Mission", "Trivia", 1) });

        // Act
        handler.Handle(session, "Active");

        // Assert
        callSequence.Should().ContainInOrder("first", "second");
    }

    [Fact]
    public void Handle_WhenValidateThrows_ShouldNotCallNext()
    {
        // Arrange
        var callSequence = new List<string>();
        var handler = new ConcreteStateHandler(() => throw new InvalidOperationException("validation failed"));
        var nextHandler = new ConcreteStateHandler(() => callSequence.Add("should not be called"));
        handler.SetNext(nextHandler);
        var session = Session.Create("Test Session", "123456", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), "Test Mission", "Trivia", 1) });

        // Act
        var act = () => handler.Handle(session, "Active");

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("validation failed");
        callSequence.Should().NotContain("should not be called");
    }

    [Fact]
    public void SetNext_ShouldReturnHandlerForChaining()
    {
        // Arrange
        var handler = new ConcreteStateHandler(() => { });
        var nextHandler = new ConcreteStateHandler(() => { });

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
        var session = Session.Create("Test Session", "123456", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), "Test Mission", "Trivia", 1) });

        // Act
        first.Handle(session, "Active");

        // Assert
        callSequence.Should().ContainInOrder(1, 2, 3);
        callSequence.Should().HaveCount(3);
    }

    private class ConcreteStateHandler : BaseStateTransitionHandler
    {
        private readonly Action _validateAction;

        public ConcreteStateHandler(Action validateAction)
        {
            _validateAction = validateAction;
        }

        protected override void Validate(Session session, string newStatus)
        {
            _validateAction();
        }
    }

    private class NumberedHandler : BaseStateTransitionHandler
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

        protected override void Validate(Session session, string newStatus)
        {
            _validateAction();
            _callSequence.Add(_number);
        }
    }
}
