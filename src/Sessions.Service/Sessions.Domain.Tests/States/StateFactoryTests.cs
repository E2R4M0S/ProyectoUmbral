using FluentAssertions;
using Sessions.Domain.Enums;
using Sessions.Domain.States;
using Xunit;

namespace Sessions.Domain.Tests.States;

public class StateFactoryTests
{
    [Theory]
    [InlineData(SessionStatus.Scheduled, typeof(ScheduledState))]
    [InlineData(SessionStatus.Preparing, typeof(PreparingState))]
    [InlineData(SessionStatus.Active, typeof(ActiveState))]
    [InlineData(SessionStatus.Paused, typeof(PausedState))]
    [InlineData(SessionStatus.Finished, typeof(FinishedState))]
    [InlineData(SessionStatus.Cancelled, typeof(CancelledState))]
    public void Create_ShouldReturnCorrectStateType(SessionStatus status, Type expectedType)
    {
        // Act
        var state = StateFactory.Create(status);

        // Assert
        state.Should().NotBeNull();
        state.Status.Should().Be(status);
        state.Should().BeOfType(expectedType);
    }

    [Fact]
    public void Create_ShouldReturnSingletonInstances()
    {
        // Act
        var state1 = StateFactory.Create(SessionStatus.Active);
        var state2 = StateFactory.Create(SessionStatus.Active);

        // Assert
        state1.Should().BeSameAs(state2);
    }

    [Fact]
    public void Create_ForAllStatuses_ShouldHaveValidState()
    {
        foreach (SessionStatus status in Enum.GetValues<SessionStatus>())
        {
            // Act
            var state = StateFactory.Create(status);

            // Assert
            state.Should().NotBeNull($"State for {status} should exist");
            state.Status.Should().Be(status);
        }
    }
}