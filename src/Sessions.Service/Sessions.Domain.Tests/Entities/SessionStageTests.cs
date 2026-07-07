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

        var stage = SessionStage.Create(missionId, Guid.NewGuid(), "Trivia Fácil", "Trivia", 1, "test-token");

        stage.MissionId.Should().Be(missionId);
        stage.MissionTitle.Should().Be("Trivia Fácil");
        stage.MissionType.Should().Be("Trivia");
        stage.Order.Should().Be(1);
    }

    [Fact]
    public void Create_WithEmptyMissionId_ShouldThrow()
    {
        Action act = () => SessionStage.Create(Guid.Empty, Guid.NewGuid(), "Title", "Trivia", 1, "test-token");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*MissionId*");
    }

    [Fact]
    public void Create_WithEmptyMissionTitle_ShouldThrow()
    {
        Action act = () => SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "", "Trivia", 1, "test-token");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*MissionTitle*");
    }

    [Fact]
    public void Create_WithWhitespaceMissionTitle_ShouldThrow()
    {
        Action act = () => SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "   ", "Trivia", 1, "test-token");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*MissionTitle*");
    }

    [Fact]
    public void Create_WithEmptyMissionType_ShouldThrow()
    {
        Action act = () => SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Title", "", 1, "test-token");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*MissionType*");
    }

    [Fact]
    public void Create_WithOrderZero_ShouldThrow()
    {
        Action act = () => SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Title", "Trivia", 0, "test-token");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Order*");
    }

    [Fact]
    public void Create_WithNegativeOrder_ShouldThrow()
    {
        Action act = () => SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Title", "Trivia", -1, "test-token");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Order*");
    }

    [Fact]
    public void Create_ShouldTrimTitleWhitespace()
    {
        var stage = SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "  Treasure Hunt  ", "Treasure", 2, "test-token");

        stage.MissionTitle.Should().Be("Treasure Hunt");
    }

    [Fact]
    public void Create_WithTreasureType_ShouldSucceed()
    {
        var stage = SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Treasure Mission", "Treasure", 3, "test-token");

        stage.MissionType.Should().Be("Treasure");
        stage.Order.Should().Be(3);
    }

    [Fact]
    public void Create_MultipleStages_AllHaveCorrectOrder()
    {
        var stages = Enumerable.Range(1, 5)
            .Select(i => SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), $"Mission {i}", "Trivia", i, "test-token"))
            .ToList();

        stages.Select(s => s.Order).Should().BeEquivalentTo([1, 2, 3, 4, 5]);
    }

    [Fact]
    public void Create_WithEmptyMissionStageId_ShouldThrow()
    {
        Action act = () => SessionStage.Create(Guid.NewGuid(), Guid.Empty, "Title", "Trivia", 1, "test-token");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*MissionStageId*");
    }

    [Fact]
    public void Create_WithEmptyQrToken_ShouldThrow()
    {
        Action act = () => SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Title", "Trivia", 1, "");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*QrToken*");
    }

    [Fact]
    public void Create_ShouldSetMissionStageIdAndQrToken()
    {
        var missionStageId = Guid.NewGuid();
        var stage = SessionStage.Create(Guid.NewGuid(), missionStageId, "Title", "Trivia", 1, "my-qr-token");

        stage.MissionStageId.Should().Be(missionStageId);
        stage.QrToken.Should().Be("my-qr-token");
    }

    [Fact]
    public void Create_WithOptionalCoordinates_ShouldSetLatLong()
    {
        var stage = SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Title", "Treasure", 1, "tok", 10.5, -66.9);

        stage.Latitude.Should().Be(10.5);
        stage.Longitude.Should().Be(-66.9);
    }

    [Fact]
    public void Create_WithoutCoordinates_ShouldHaveNullLatLong()
    {
        var stage = SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Title", "Treasure", 1, "tok");

        stage.Latitude.Should().BeNull();
        stage.Longitude.Should().BeNull();
    }

    [Fact]
    public void ValidateQrToken_WithMatchingStageIdAndToken_ShouldReturnTrue()
    {
        var stageId = Guid.NewGuid();
        var stage = SessionStage.Create(Guid.NewGuid(), stageId, "Title", "Treasure", 1, "correct-token");

        stage.ValidateQrToken(stageId, "correct-token").Should().BeTrue();
    }

    [Fact]
    public void ValidateQrToken_WithWrongToken_ShouldReturnFalse()
    {
        var stageId = Guid.NewGuid();
        var stage = SessionStage.Create(Guid.NewGuid(), stageId, "Title", "Treasure", 1, "correct-token");

        stage.ValidateQrToken(stageId, "wrong-token").Should().BeFalse();
    }

    [Fact]
    public void ValidateQrToken_WithWrongStageId_ShouldReturnFalse()
    {
        var stageId = Guid.NewGuid();
        var stage = SessionStage.Create(Guid.NewGuid(), stageId, "Title", "Treasure", 1, "correct-token");

        stage.ValidateQrToken(Guid.NewGuid(), "correct-token").Should().BeFalse();
    }

    [Fact]
    public void ValidateQrToken_WithBothWrong_ShouldReturnFalse()
    {
        var stage = SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Title", "Treasure", 1, "correct-token");

        stage.ValidateQrToken(Guid.NewGuid(), "wrong-token").Should().BeFalse();
    }
}
