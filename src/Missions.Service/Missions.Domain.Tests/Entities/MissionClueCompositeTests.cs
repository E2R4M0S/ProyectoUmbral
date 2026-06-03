using FluentAssertions;
using Missions.Domain.Entities;
using Missions.Domain.Enums;
using Xunit;

namespace Missions.Domain.Tests.Entities;

public class MissionClueCompositeTests
{
    [Fact]
    public void GetTotalPenalty_WithValue10_Returns10()
    {
        var clue = new MissionClue(Guid.NewGuid(), "Valid content", 10, ReleaseType.Auto);

        var penalty = clue.GetTotalPenalty();

        penalty.Should().Be(10);
    }

    [Fact]
    public void GetTotalPenalty_WithNull_Returns0()
    {
        var clue = new MissionClue(Guid.NewGuid(), "Valid content", null, ReleaseType.Auto);

        var penalty = clue.GetTotalPenalty();

        penalty.Should().Be(0);
    }

    [Fact]
    public void GetLeafCount_Returns1()
    {
        var clue = new MissionClue(Guid.NewGuid(), "Valid content", 5, ReleaseType.Manual);

        var count = clue.GetLeafCount();

        count.Should().Be(1);
    }

    [Fact]
    public void Validate_WithNonEmptyContent_ReturnsValid()
    {
        var clue = new MissionClue(Guid.NewGuid(), "Clue content is valid", 5, ReleaseType.Auto);

        var result = clue.Validate();

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithEmptyContent_ReturnsInvalid()
    {
        var clue = new MissionClue(Guid.NewGuid(), "", 5, ReleaseType.Auto);

        var result = clue.Validate();

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("content", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void MissionClue_ImplementsIMissionComponent()
    {
        var clue = new MissionClue(Guid.NewGuid(), "Content", null, ReleaseType.Auto);

        clue.Should().BeAssignableTo<IMissionComponent>();
    }
}