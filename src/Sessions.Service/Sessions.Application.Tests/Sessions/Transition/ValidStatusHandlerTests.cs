using FluentAssertions;
using Sessions.Application.Sessions.Transition.Chain;
using Sessions.Domain.Entities;
using Sessions.Domain.Enums;
using Xunit;

namespace Sessions.Application.Tests.Sessions.Transition;

public class ValidStatusHandlerTests
{
    [Fact]
    public void Handle_WithValidStatus_ShouldNotThrow()
    {
        // Arrange
        var handler = new ValidStatusHandler();
        var session = Session.Create("Test Session", "123456", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), "Test Mission", "Trivia", 1) });

        // Act
        var act = () => handler.Handle(session, "Active");

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("Scheduled")]
    [InlineData("Preparing")]
    [InlineData("Active")]
    [InlineData("Paused")]
    [InlineData("Finished")]
    [InlineData("Cancelled")]
    public void Handle_WithAllValidStatuses_ShouldNotThrow(string status)
    {
        // Arrange
        var handler = new ValidStatusHandler();
        var session = Session.Create("Test Session", "123456", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), "Test Mission", "Trivia", 1) });

        // Act
        var act = () => handler.Handle(session, status);

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
        var handler = new ValidStatusHandler();
        var session = Session.Create("Test Session", "123456", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), "Test Mission", "Trivia", 1) });

        // Act
        var act = () => handler.Handle(session, invalidStatus);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*'{invalidStatus}' no es un estado válido*");
    }

    [Fact]
    public void Handle_ShouldBeCaseInsensitive()
    {
        // Arrange
        var handler = new ValidStatusHandler();
        var session = Session.Create("Test Session", "123456", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), "Test Mission", "Trivia", 1) });

        // Act
        var act = () => handler.Handle(session, "active");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void SetNext_ShouldReturnHandlerForChaining()
    {
        // Arrange
        var handler = new ValidStatusHandler();
        var nextHandler = new MockNextHandler();

        // Act
        var result = handler.SetNext(nextHandler);

        // Assert
        result.Should().Be(nextHandler);
    }

    private class MockNextHandler : IStateTransitionHandler
    {
        public IStateTransitionHandler SetNext(IStateTransitionHandler handler) => handler;
        public void Handle(Session session, string newStatus) { }
    }
}