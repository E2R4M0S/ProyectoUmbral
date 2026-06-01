using FluentAssertions;
using Missions.Domain.Entities;
using Missions.Domain.Enums;
using Xunit;

namespace Missions.Domain.Tests.Entities;

public class MissionStageCompositeTests
{
    [Fact]
    public void GetTotalPenalty_Sums3Clues_Returns15()
    {
        // Arrange: stage with 3 clues (penalties 5, 10, null)
        var stage = CreateStageWithClues(5, 10, null);

        // Act
        var penalty = stage.GetTotalPenalty();

        // Assert
        penalty.Should().Be(15);
    }

    [Fact]
    public void GetLeafCount_4Clues_Returns4()
    {
        // Arrange: stage with 4 clues
        var stage = CreateStageWithClues(1, 2, 3, 4);

        // Act
        var count = stage.GetLeafCount();

        // Assert
        count.Should().Be(4);
    }

    [Fact]
    public void GetTotalPenalty_EmptyStage_Returns0()
    {
        // Arrange: empty stage
        var stage = CreateEmptyStage();

        // Act
        var penalty = stage.GetTotalPenalty();

        // Assert
        penalty.Should().Be(0);
    }

    [Fact]
    public void GetLeafCount_EmptyStage_Returns0()
    {
        // Arrange: empty stage
        var stage = CreateEmptyStage();

        // Act
        var count = stage.GetLeafCount();

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public void Validate_AllCluesValid_ReturnsValid()
    {
        // Arrange: stage with 2 valid clues
        var stage = CreateStageWithClues(5, 10);

        // Act
        var result = stage.Validate();

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_InvalidClueCascades_ReturnsInvalid()
    {
        // Arrange: stage with 1 valid clue and 1 invalid clue (empty content)
        var stage = CreateStageWithClues(5);
        var stageId = stage.Id;
        // Manually add invalid clue via reflection since internal ctor doesn't validate
        AddInvalidClueToStage(stage);

        // Act
        var result = stage.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void Validate_EmptyName_ReturnsInvalid()
    {
        // Arrange: stage with valid clues but empty name
        var stage = new MissionStage(Guid.NewGuid(), "", "Description", 1);
        AddValidClueToStage(stage, "Valid content", 5);

        // Act
        var result = stage.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("name", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_RequiresAtLeastOneClue_ReturnsInvalidWhenEmpty()
    {
        // Arrange: empty stage
        var stage = CreateEmptyStage();

        // Act
        var result = stage.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("clue", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void MissionStage_ImplementsIMissionComponent()
    {
        var stage = CreateEmptyStage();

        stage.Should().BeAssignableTo<IMissionComponent>();
    }

    private static MissionStage CreateStageWithClues(params int?[] penalties)
    {
        var stage = new MissionStage(Guid.NewGuid(), "Test Stage", "Test description", 1);
        foreach (var penalty in penalties)
        {
            AddValidClueToStage(stage, "Clue content", penalty);
        }
        return stage;
    }

    private static MissionStage CreateEmptyStage()
    {
        return new MissionStage(Guid.NewGuid(), "Test Stage", "Test description", 1);
    }

    private static void AddValidClueToStage(MissionStage stage, string content, int? penalty)
    {
        var addMethod = typeof(MissionStage).GetMethod("AddClue",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        addMethod?.Invoke(stage, new object[] { content, penalty, ReleaseType.Auto });
    }

    private static void AddInvalidClueToStage(MissionStage stage)
    {
        // Use reflection to add a clue with empty content
        var field = typeof(MissionStage).GetField("_clues",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var clues = (List<MissionClue>?)field?.GetValue(stage);
        if (clues != null)
        {
            var invalidClue = new MissionClue(stage.Id, "", 5, ReleaseType.Auto);
            clues.Add(invalidClue);
        }
    }
}