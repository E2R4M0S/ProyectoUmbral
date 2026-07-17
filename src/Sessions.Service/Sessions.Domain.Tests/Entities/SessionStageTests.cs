using FluentAssertions;
using Sessions.Domain.Entities;
using Xunit;

namespace Sessions.Domain.Tests.Entities;

public class SessionStageTests
{
    [Fact]
    public void Create_WithValidInputs_ShouldSetAllProperties()
    {
        var missionId = Guid.NewGuid();

        var stage = SessionStage.Create(missionId, Guid.NewGuid(), "Trivia Fácil", "Stage", "Trivia", 1, "test-token");

        stage.MissionId.Should().Be(missionId);
        stage.MissionTitle.Should().Be("Trivia Fácil");
        stage.MissionType.Should().Be("Trivia");
        stage.Order.Should().Be(1);
    }

    [Fact]
    public void Create_WithEmptyMissionId_ShouldThrow()
    {
        Action act = () => SessionStage.Create(Guid.Empty, Guid.NewGuid(), "Title", "Stage", "Trivia", 1, "test-token");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*MissionId*");
    }

    [Fact]
    public void Create_WithEmptyMissionTitle_ShouldThrow()
    {
        Action act = () => SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "", "Stage", "Trivia", 1, "test-token");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*MissionTitle*");
    }

    [Fact]
    public void Create_WithWhitespaceMissionTitle_ShouldThrow()
    {
        Action act = () => SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "   ", "Stage", "Trivia", 1, "test-token");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*MissionTitle*");
    }

    [Fact]
    public void Create_WithEmptyMissionType_ShouldThrow()
    {
        Action act = () => SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Title", "Stage", "", 1, "test-token");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*MissionType*");
    }

    [Fact]
    public void Create_WithOrderZero_ShouldThrow()
    {
        Action act = () => SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Title", "Stage", "Trivia", 0, "test-token");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Order*");
    }

    [Fact]
    public void Create_WithNegativeOrder_ShouldThrow()
    {
        Action act = () => SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Title", "Stage", "Trivia", -1, "test-token");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Order*");
    }

    [Fact]
    public void Create_ShouldTrimTitleWhitespace()
    {
        var stage = SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "  Treasure Hunt  ", "Stage", "Treasure", 2, "test-token");

        stage.MissionTitle.Should().Be("Treasure Hunt");
    }

    [Fact]
    public void Create_WithTreasureType_ShouldSucceed()
    {
        var stage = SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Treasure Mission", "Stage", "Treasure", 3, "test-token");

        stage.MissionType.Should().Be("Treasure");
        stage.Order.Should().Be(3);
    }

    [Fact]
    public void Create_MultipleStages_AllHaveCorrectOrder()
    {
        var stages = Enumerable.Range(1, 5)
            .Select(i => SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), $"Mission {i}", "Stage", "Trivia", i, "test-token"))
            .ToList();

        stages.Select(s => s.Order).Should().BeEquivalentTo([1, 2, 3, 4, 5]);
    }

    [Fact]
    public void Create_WithEmptyMissionStageId_ShouldThrow()
    {
        Action act = () => SessionStage.Create(Guid.NewGuid(), Guid.Empty, "Title", "Stage", "Treasure", 1, "test-token");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*MissionStageId*");
    }

    [Fact]
    public void Create_WithEmptyQrToken_ShouldThrow()
    {
        Action act = () => SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Title", "Stage", "Treasure", 1, "");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*QrToken*");
    }

    [Fact]
    public void Create_ShouldSetMissionStageIdAndQrToken()
    {
        var missionStageId = Guid.NewGuid();
        var stage = SessionStage.Create(Guid.NewGuid(), missionStageId, "Title", "Stage", "Trivia", 1, "my-qr-token");

        stage.MissionStageId.Should().Be(missionStageId);
        stage.QrToken.Should().Be("my-qr-token");
    }

    [Fact]
    public void Create_WithOptionalCoordinates_ShouldSetLatLong()
    {
        var stage = SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Title", "Stage", "Treasure", 1, "tok", 0, 10.5, -66.9);

        stage.Latitude.Should().Be(10.5);
        stage.Longitude.Should().Be(-66.9);
    }

    [Fact]
    public void Create_WithoutCoordinates_ShouldHaveNullLatLong()
    {
        var stage = SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Title", "Stage", "Treasure", 1, "tok");

        stage.Latitude.Should().BeNull();
        stage.Longitude.Should().BeNull();
    }

    [Fact]
    public void ValidateQrToken_WithMatchingStageIdAndToken_ShouldReturnTrue()
    {
        var stageId = Guid.NewGuid();
        var stage = SessionStage.Create(Guid.NewGuid(), stageId, "Title", "Stage", "Treasure", 1, "correct-token");

        stage.ValidateQrToken(stageId, "correct-token").Should().BeTrue();
    }

    [Fact]
    public void ValidateQrToken_WithWrongToken_ShouldReturnFalse()
    {
        var stageId = Guid.NewGuid();
        var stage = SessionStage.Create(Guid.NewGuid(), stageId, "Title", "Stage", "Treasure", 1, "correct-token");

        stage.ValidateQrToken(stageId, "wrong-token").Should().BeFalse();
    }

    [Fact]
    public void ValidateQrToken_WithWrongStageId_ShouldReturnFalse()
    {
        var stageId = Guid.NewGuid();
        var stage = SessionStage.Create(Guid.NewGuid(), stageId, "Title", "Stage", "Treasure", 1, "correct-token");

        stage.ValidateQrToken(Guid.NewGuid(), "correct-token").Should().BeFalse();
    }

    [Fact]
    public void ValidateQrToken_WithBothWrong_ShouldReturnFalse()
    {
        var stage = SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Title", "Stage", "Treasure", 1, "correct-token");

        stage.ValidateQrToken(Guid.NewGuid(), "wrong-token").Should().BeFalse();
    }

    [Fact]
    public void Create_WithoutDifficulty_ShouldDefaultToMedium()
    {
        var stage = SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Title", "Stage", "Treasure", 1, "tok");

        stage.Difficulty.Should().Be("Medium");
        stage.BaseScanPoints.Should().Be(150);
    }

    [Theory]
    [InlineData("Easy", 100)]
    [InlineData("Medium", 150)]
    [InlineData("Hard", 200)]
    public void BaseScanPoints_ShouldScaleWithDifficulty(string difficulty, int expectedPoints)
    {
        var stage = SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Title", "Stage", "Treasure", 1, "tok", difficulty: difficulty);

        stage.BaseScanPoints.Should().Be(expectedPoints);
    }
}
