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

        var stage = SessionStage.Create(missionId, "Trivia Fácil", "Trivia", 1);

        stage.MissionId.Should().Be(missionId);
        stage.MissionTitle.Should().Be("Trivia Fácil");
        stage.MissionType.Should().Be("Trivia");
        stage.Order.Should().Be(1);
    }

    [Fact]
    public void Create_WithEmptyMissionId_ShouldThrow()
    {
        Action act = () => SessionStage.Create(Guid.Empty, "Title", "Trivia", 1);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*MissionId*");
    }

    [Fact]
    public void Create_WithEmptyMissionTitle_ShouldThrow()
    {
        Action act = () => SessionStage.Create(Guid.NewGuid(), "", "Trivia", 1);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*MissionTitle*");
    }

    [Fact]
    public void Create_WithWhitespaceMissionTitle_ShouldThrow()
    {
        Action act = () => SessionStage.Create(Guid.NewGuid(), "   ", "Trivia", 1);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*MissionTitle*");
    }

    [Fact]
    public void Create_WithEmptyMissionType_ShouldThrow()
    {
        Action act = () => SessionStage.Create(Guid.NewGuid(), "Title", "", 1);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*MissionType*");
    }

    [Fact]
    public void Create_WithOrderZero_ShouldThrow()
    {
        Action act = () => SessionStage.Create(Guid.NewGuid(), "Title", "Trivia", 0);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Order*");
    }

    [Fact]
    public void Create_WithNegativeOrder_ShouldThrow()
    {
        Action act = () => SessionStage.Create(Guid.NewGuid(), "Title", "Trivia", -1);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Order*");
    }

    [Fact]
    public void Create_ShouldTrimTitleWhitespace()
    {
        var stage = SessionStage.Create(Guid.NewGuid(), "  Treasure Hunt  ", "Treasure", 2);

        stage.MissionTitle.Should().Be("Treasure Hunt");
    }

    [Fact]
    public void Create_WithTreasureType_ShouldSucceed()
    {
        var stage = SessionStage.Create(Guid.NewGuid(), "Treasure Mission", "Treasure", 3);

        stage.MissionType.Should().Be("Treasure");
        stage.Order.Should().Be(3);
    }

    [Fact]
    public void Create_MultipleStages_AllHaveCorrectOrder()
    {
        var stages = Enumerable.Range(1, 5)
            .Select(i => SessionStage.Create(Guid.NewGuid(), $"Mission {i}", "Trivia", i))
            .ToList();

        stages.Select(s => s.Order).Should().BeEquivalentTo([1, 2, 3, 4, 5]);
    }
}
