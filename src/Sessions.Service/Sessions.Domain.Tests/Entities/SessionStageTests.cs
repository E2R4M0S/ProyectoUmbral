using FluentAssertions;
using Sessions.Domain.Entities;
using Xunit;

namespace Sessions.Domain.Tests.Entities;

public class SessionStageTests
{
    [Fact]
    public void Create_WithValidInputs_ShouldSetAllProperties()
    {
        // Arrange
        var missionId = Guid.NewGuid();

        // Act
        var stage = SessionStage.Create(missionId, "Trivia Facil", "Trivia", 1);

        // Assert
        stage.MissionId.Should().Be(missionId);
        stage.MissionTitle.Should().Be("Trivia Facil");
        stage.MissionType.Should().Be("Trivia");
        stage.Order.Should().Be(1);
    }

    [Fact]
    public void Create_WithEmptyMissionId_ShouldThrow()
    {
        // Act
        Action act = () => SessionStage.Create(Guid.Empty, "Title", "Trivia", 1);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*MissionId*");
    }

    [Fact]
    public void Create_WithEmptyMissionTitle_ShouldThrow()
    {
        // Act
        Action act = () => SessionStage.Create(Guid.NewGuid(), "", "Trivia", 1);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*MissionTitle*");
    }

    [Fact]
    public void Create_WithWhitespaceMissionTitle_ShouldThrow()
    {
        // Act
        Action act = () => SessionStage.Create(Guid.NewGuid(), "   ", "Trivia", 1);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*MissionTitle*");
    }

    [Fact]
    public void Create_WithEmptyMissionType_ShouldThrow()
    {
        // Act
        Action act = () => SessionStage.Create(Guid.NewGuid(), "Title", "", 1);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*MissionType*");
    }

    [Fact]
    public void Create_WithNonPositiveOrder_ShouldThrow()
    {
        // Act
        Action act = () => SessionStage.Create(Guid.NewGuid(), "Title", "Trivia", 0);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Order*");
    }

    [Fact]
    public void Create_ShouldTrimTitleWhitespace()
    {
        // Act
        var stage = SessionStage.Create(Guid.NewGuid(), "  Treasure Hunt  ", "Treasure", 2);

        // Assert
        stage.MissionTitle.Should().Be("Treasure Hunt");
    }
}
