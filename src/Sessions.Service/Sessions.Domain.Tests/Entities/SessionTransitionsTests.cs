using FluentAssertions;
using Sessions.Domain.Entities;
using Sessions.Domain.Enums;
using Xunit;

namespace Sessions.Domain.Tests.Entities;

public class SessionTransitionsTests
{
    [Theory]
    [InlineData(SessionStatus.Scheduled, SessionStatus.Preparing)]
    [InlineData(SessionStatus.Scheduled, SessionStatus.Cancelled)]
    [InlineData(SessionStatus.Preparing, SessionStatus.Active)]
    [InlineData(SessionStatus.Preparing, SessionStatus.Cancelled)]
    [InlineData(SessionStatus.Active, SessionStatus.Paused)]
    [InlineData(SessionStatus.Active, SessionStatus.Finished)]
    [InlineData(SessionStatus.Active, SessionStatus.Cancelled)]
    [InlineData(SessionStatus.Paused, SessionStatus.Active)]
    [InlineData(SessionStatus.Paused, SessionStatus.Finished)]
    [InlineData(SessionStatus.Paused, SessionStatus.Cancelled)]
    public void TransitionTo_WithValidTransition_ShouldUpdateStatus(
        SessionStatus from, SessionStatus to)
    {
        // Arrange
        var session = CreateSessionWithStatus(from);

        // Act
        session.TransitionTo(to);

        // Assert
        session.Status.Should().Be(to);
    }

    [Theory]
    [InlineData(SessionStatus.Scheduled, SessionStatus.Active)]
    [InlineData(SessionStatus.Scheduled, SessionStatus.Paused)]
    [InlineData(SessionStatus.Scheduled, SessionStatus.Finished)]
    [InlineData(SessionStatus.Preparing, SessionStatus.Scheduled)]
    [InlineData(SessionStatus.Preparing, SessionStatus.Paused)]
    [InlineData(SessionStatus.Preparing, SessionStatus.Finished)]
    [InlineData(SessionStatus.Active, SessionStatus.Scheduled)]
    [InlineData(SessionStatus.Active, SessionStatus.Preparing)]
    [InlineData(SessionStatus.Paused, SessionStatus.Scheduled)]
    [InlineData(SessionStatus.Paused, SessionStatus.Preparing)]
    public void TransitionTo_WithInvalidTransition_ShouldThrowInvalidOperationException(
        SessionStatus from, SessionStatus to)
    {
        // Arrange
        var session = CreateSessionWithStatus(from);

        // Act
        var act = () => session.TransitionTo(to);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(SessionStatus.Finished)]
    [InlineData(SessionStatus.Cancelled)]
    public void TransitionTo_FromTerminalState_ShouldThrowInvalidOperationException(
        SessionStatus terminalState)
    {
        // Arrange
        var session = CreateSessionWithStatus(terminalState);

        // Act
        var act = () => session.TransitionTo(SessionStatus.Active);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(SessionStatus.Scheduled, SessionStatus.Cancelled)]
    [InlineData(SessionStatus.Preparing, SessionStatus.Cancelled)]
    [InlineData(SessionStatus.Active, SessionStatus.Finished)]
    [InlineData(SessionStatus.Active, SessionStatus.Cancelled)]
    [InlineData(SessionStatus.Paused, SessionStatus.Finished)]
    [InlineData(SessionStatus.Paused, SessionStatus.Cancelled)]
    public void TransitionTo_ToTerminalState_ShouldUpdateStatus(
        SessionStatus from, SessionStatus to)
    {
        // Arrange
        var session = CreateSessionWithStatus(from);

        // Act
        session.TransitionTo(to);

        // Assert
        session.Status.Should().Be(to);
    }

    [Fact]
    public void TransitionTo_ToActive_FromScheduled_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var session = CreateSessionWithStatus(SessionStatus.Scheduled);
        session.StartedAt.Should().BeNull("StartedAt should not be set before activation");

        // Act
        var act = () => session.TransitionTo(SessionStatus.Active);

        // Assert - Scheduled cannot transition directly to Active
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void TransitionTo_ToActive_FromPreparing_ShouldSetStartedAt()
    {
        // Arrange
        var session = CreateSessionWithStatus(SessionStatus.Preparing);
        session.StartedAt.Should().BeNull("StartedAt should not be set before activation");

        // Act
        session.TransitionTo(SessionStatus.Active);

        // Assert
        session.StartedAt.Should().NotBeNull();
        session.StartedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void TransitionTo_ToActive_WhenAlreadyActive_ShouldNotOverwriteStartedAt()
    {
        // Arrange - start with Preparing -> Active (valid first activation)
        var session = CreateSessionWithStatus(SessionStatus.Preparing);
        session.TransitionTo(SessionStatus.Active);
        var firstStartedAt = session.StartedAt;

        // Wait a small amount to ensure time difference
        Thread.Sleep(10);

        // Act - transition Active -> Paused -> Active again
        session.TransitionTo(SessionStatus.Paused);
        session.TransitionTo(SessionStatus.Active);

        // Assert - StartedAt should remain the original value
        session.StartedAt.Should().Be(firstStartedAt);
    }

    [Theory]
    [InlineData(SessionStatus.Finished)]
    [InlineData(SessionStatus.Cancelled)]
    public void TransitionTo_ToTerminalState_ShouldSetEndedAt(SessionStatus terminalStatus)
    {
        // Arrange
        var session = CreateSessionWithStatus(SessionStatus.Active);
        session.EndedAt.Should().BeNull("EndedAt should not be set before termination");

        // Act
        session.TransitionTo(terminalStatus);

        // Assert
        session.EndedAt.Should().NotBeNull();
        session.EndedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void TransitionTo_ToSameStatus_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var session = CreateSessionWithStatus(SessionStatus.Active);

        // Act
        var act = () => session.TransitionTo(SessionStatus.Active);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    private static Session CreateSessionWithStatus(SessionStatus status)
    {
        var session = Session.Create("Test Session", "123456", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), "Test Mission", "Trivia", 1) });

        // Use reflection to set the status since it's private
        var statusField = typeof(Session).GetProperty("Status");
        statusField!.SetValue(session, status);

        return session;
    }
}