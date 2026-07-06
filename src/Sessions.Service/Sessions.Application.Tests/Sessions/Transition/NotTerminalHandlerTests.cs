using FluentAssertions;
using Sessions.Application.Sessions.Transition.Chain;
using Sessions.Domain.Entities;
using Sessions.Domain.Enums;
using Xunit;

namespace Sessions.Application.Tests.Sessions.Transition;

public class NotTerminalHandlerTests
{
    [Fact]
    public void Handle_WithNonTerminalStatus_ShouldNotThrow()
    {
        // Arrange
        var handler = new NotTerminalHandler();
        var session = Session.Create("Test Session", "123456", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Test Mission", "Trivia", 1, Guid.NewGuid().ToString("N")) });

        // Act
        var act = () => handler.Handle(session, "Active");

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(SessionStatus.Scheduled)]
    [InlineData(SessionStatus.Preparing)]
    [InlineData(SessionStatus.Active)]
    [InlineData(SessionStatus.Paused)]
    public void Handle_FromNonTerminal_ShouldNotThrow(SessionStatus currentStatus)
    {
        // Arrange
        var handler = new NotTerminalHandler();
        var session = Session.Create("Test Session", "123456", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Test Mission", "Trivia", 1, Guid.NewGuid().ToString("N")) });
        SetSessionStatus(session, currentStatus);

        // Act
        var act = () => handler.Handle(session, "Active");

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(SessionStatus.Finished)]
    [InlineData(SessionStatus.Cancelled)]
    public void Handle_FromTerminalStatus_ShouldThrowInvalidOperationException(SessionStatus terminalStatus)
    {
        // Arrange
        var handler = new NotTerminalHandler();
        var session = Session.Create("Test Session", "123456", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Test Mission", "Trivia", 1, Guid.NewGuid().ToString("N")) });
        SetSessionStatus(session, terminalStatus);

        // Act
        var act = () => handler.Handle(session, "Active");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*La sesión ya está en estado '{terminalStatus}' y no puede transicionar*");
    }

    [Fact]
    public void SetNext_ShouldReturnHandlerForChaining()
    {
        // Arrange
        var handler = new NotTerminalHandler();
        var nextHandler = new MockNextHandler();

        // Act
        var result = handler.SetNext(nextHandler);

        // Assert
        result.Should().Be(nextHandler);
    }

    private static void SetSessionStatus(Session session, SessionStatus status)
    {
        var statusProperty = typeof(Session).GetProperty("Status")!;
        statusProperty.SetValue(session, status);
    }

    private class MockNextHandler : IStateTransitionHandler
    {
        public IStateTransitionHandler SetNext(IStateTransitionHandler handler) => handler;
        public void Handle(Session session, string newStatus) { }
    }
}