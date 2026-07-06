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
        var missionStageId = Guid.NewGuid();
        var qrToken = Guid.NewGuid().ToString("N");

        // Act
        var stage = SessionStage.Create(missionId, missionStageId, "Trivia Facil", "Trivia", 1, qrToken);

        // Assert
        stage.MissionId.Should().Be(missionId);
        stage.MissionStageId.Should().Be(missionStageId);
        stage.MissionTitle.Should().Be("Trivia Facil");
        stage.MissionType.Should().Be("Trivia");
        stage.Order.Should().Be(1);
        stage.QrToken.Should().Be(qrToken);
    }

    [Fact]
    public void Create_WithEmptyMissionId_ShouldThrow()
    {
        // Act
        Action act = () => SessionStage.Create(Guid.Empty, Guid.NewGuid(), "Title", "Trivia", 1, Guid.NewGuid().ToString("N"));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*MissionId*");
    }

    [Fact]
    public void Create_WithEmptyMissionStageId_ShouldThrow()
    {
        // Act
        Action act = () => SessionStage.Create(Guid.NewGuid(), Guid.Empty, "Title", "Trivia", 1, Guid.NewGuid().ToString("N"));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*MissionStageId*");
    }

    [Fact]
    public void Create_WithEmptyMissionTitle_ShouldThrow()
    {
        // Act
        Action act = () => SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "", "Trivia", 1, Guid.NewGuid().ToString("N"));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*MissionTitle*");
    }

    [Fact]
    public void Create_WithWhitespaceMissionTitle_ShouldThrow()
    {
        // Act
        Action act = () => SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "   ", "Trivia", 1, Guid.NewGuid().ToString("N"));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*MissionTitle*");
    }

    [Fact]
    public void Create_WithEmptyMissionType_ShouldThrow()
    {
        // Act
        Action act = () => SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Title", "", 1, Guid.NewGuid().ToString("N"));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*MissionType*");
    }

    [Fact]
    public void Create_WithNonPositiveOrder_ShouldThrow()
    {
        // Act
        Action act = () => SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Title", "Trivia", 0, Guid.NewGuid().ToString("N"));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Order*");
    }

    [Fact]
    public void Create_WithEmptyQrToken_ShouldThrow()
    {
        // Act
        Action act = () => SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Title", "Trivia", 1, "");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*QrToken*");
    }

    [Fact]
    public void Create_ShouldTrimTitleWhitespace()
    {
        // Act
        var stage = SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "  Treasure Hunt  ", "Treasure", 2, Guid.NewGuid().ToString("N"));

        // Assert
        stage.MissionTitle.Should().Be("Treasure Hunt");
    }

    [Fact]
    public void ValidateQrToken_WithMatchingIds_ShouldReturnTrue()
    {
        // Arrange
        var missionStageId = Guid.NewGuid();
        var qrToken = Guid.NewGuid().ToString("N");
        var stage = SessionStage.Create(Guid.NewGuid(), missionStageId, "Title", "Trivia", 1, qrToken);

        // Act & Assert
        stage.ValidateQrToken(missionStageId, qrToken).Should().BeTrue();
    }

    [Fact]
    public void ValidateQrToken_WithWrongToken_ShouldReturnFalse()
    {
        // Arrange
        var missionStageId = Guid.NewGuid();
        var stage = SessionStage.Create(Guid.NewGuid(), missionStageId, "Title", "Trivia", 1, Guid.NewGuid().ToString("N"));

        // Act & Assert
        stage.ValidateQrToken(missionStageId, "wrongtoken").Should().BeFalse();
    }
}
