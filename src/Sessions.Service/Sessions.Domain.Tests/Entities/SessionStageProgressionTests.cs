using FluentAssertions;
using Sessions.Domain.Entities;
using Sessions.Domain.Enums;
using Xunit;

namespace Sessions.Domain.Tests.Entities;

public class SessionStageProgressionTests
{
    private static Session CreateWithStages(int stageCount)
    {
        var stages = Enumerable.Range(1, stageCount)
            .Select(i => SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), $"Mission {i}", "Trivia", i, Guid.NewGuid().ToString("N")))
            .ToList();
        return Session.Create("Test Session", "123456", stages);
    }

    [Fact]
    public void Create_WithStages_ShouldInitializeCurrentStageOrderAtZero()
    {
        // Act
        var session = CreateWithStages(3);

        // Assert
        session.CurrentStageOrder.Should().Be(0);
        session.Stages.Should().HaveCount(3);
    }

    [Fact]
    public void GetCurrentStage_OnFreshSession_ShouldReturnFirstStage()
    {
        // Act
        var session = CreateWithStages(3);

        // Assert
        session.GetCurrentStage().Should().NotBeNull();
        session.GetCurrentStage()!.Order.Should().Be(1);
        session.GetCurrentStage()!.MissionTitle.Should().Be("Mission 1");
    }

    [Fact]
    public void GetCurrentStage_OnActiveSession_ShouldReturnStageAtCurrentStageOrder()
    {
        // Arrange
        var session = CreateWithStages(3);
        session.TransitionTo(SessionStatus.Preparing);
        session.TransitionTo(SessionStatus.Active);

        // Act
        session.AdvanceStage();
        var current = session.GetCurrentStage();

        // Assert
        current.Should().NotBeNull();
        current!.Order.Should().Be(2);
        current.MissionTitle.Should().Be("Mission 2");
        session.CurrentStageOrder.Should().Be(1);
    }

    [Fact]
    public void AdvanceStage_WhenActive_ShouldIncrementCurrentStageOrder()
    {
        // Arrange
        var session = CreateWithStages(3);
        session.TransitionTo(SessionStatus.Preparing);
        session.TransitionTo(SessionStatus.Active);
        var before = session.CurrentStageOrder;

        // Act
        session.AdvanceStage();

        // Assert
        session.CurrentStageOrder.Should().Be(before + 1);
    }

    [Fact]
    public void AdvanceStage_OnLastStage_ShouldThrow()
    {
        // Arrange
        var session = CreateWithStages(2);
        session.TransitionTo(SessionStatus.Preparing);
        session.TransitionTo(SessionStatus.Active);
        session.AdvanceStage(); // now at last stage (order=2 of 2)

        // Act
        Action act = () => session.AdvanceStage();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*last stage*");
    }

    [Fact]
    public void AdvanceStage_WhenNotActive_ShouldThrow()
    {
        // Arrange — session in Scheduled state
        var session = CreateWithStages(3);

        // Act
        Action act = () => session.AdvanceStage();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*active*");
    }

    [Fact]
    public void AdvanceStage_WhenPreparing_ShouldThrow()
    {
        // Arrange
        var session = CreateWithStages(3);
        session.TransitionTo(SessionStatus.Preparing);

        // Act
        Action act = () => session.AdvanceStage();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*active*");
    }

    [Fact]
    public void Create_WithEmptyStages_ShouldThrow()
    {
        // Act
        Action act = () => Session.Create("Test", "123456", new List<SessionStage>());

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*stage*");
    }

    [Fact]
    public void Create_WithDuplicateOrder_ShouldThrow()
    {
        // Arrange
        var stages = new List<SessionStage>
        {
            SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "M1", "Trivia", 1, Guid.NewGuid().ToString("N")),
            SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "M2", "Treasure", 1, Guid.NewGuid().ToString("N")), // duplicate order
        };

        // Act
        Action act = () => Session.Create("Test", "123456", stages);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*duplicate Order*");
    }

    [Fact]
    public void Create_WithNonSequentialOrder_ShouldThrow()
    {
        // Arrange
        var stages = new List<SessionStage>
        {
            SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "M1", "Trivia", 1, Guid.NewGuid().ToString("N")),
            SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "M2", "Treasure", 3, Guid.NewGuid().ToString("N")), // missing 2
        };

        // Act
        Action act = () => Session.Create("Test", "123456", stages);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*sequential*");
    }

    [Fact]
    public void Create_WithSingleStage_ShouldHaveOneStage()
    {
        // Arrange
        var stages = new List<SessionStage>
        {
            SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Solo", "Trivia", 1, Guid.NewGuid().ToString("N"))
        };

        // Act
        var session = Session.Create("Test", "123456", stages);

        // Assert
        session.Stages.Should().HaveCount(1);
        session.CurrentStageOrder.Should().Be(0);
    }
}
