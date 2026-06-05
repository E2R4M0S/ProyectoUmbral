using Xunit;
using FluentAssertions;
using Trivia.Domain.Entities;

namespace Trivia.Domain.Tests.Entities;

public class LeaderboardEntryTests
{
    [Fact]
    public void DefaultConstruction_ShouldSetDefaultValues()
    {
        var entry = new LeaderboardEntry();

        entry.Score.Should().Be(0);
        entry.UpdatedAt.Should().Be(default);
    }

    [Fact]
    public void Properties_ShouldBeSettableAndGettable()
    {
        var id = Guid.NewGuid();
        var quizId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var score = 100;
        var updatedAt = DateTime.UtcNow;

        var entry = new LeaderboardEntry
        {
            Id = id,
            QuizId = quizId,
            TeamId = teamId,
            TeamName = "Team Alpha",
            Score = score,
            UpdatedAt = updatedAt
        };

        entry.Id.Should().Be(id);
        entry.QuizId.Should().Be(quizId);
        entry.TeamId.Should().Be(teamId);
        entry.TeamName.Should().Be("Team Alpha");
        entry.Score.Should().Be(score);
        entry.UpdatedAt.Should().Be(updatedAt);
    }

    [Fact]
    public void Score_ShouldBeManipulable()
    {
        var entry = new LeaderboardEntry { Score = 50 };

        entry.Score = 75;
        entry.Score.Should().Be(75);

        entry.Score += 25;
        entry.Score.Should().Be(100);
    }
}