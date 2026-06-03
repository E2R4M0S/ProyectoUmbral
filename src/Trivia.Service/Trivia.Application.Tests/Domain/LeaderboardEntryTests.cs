using Xunit;
using FluentAssertions;
using Trivia.Domain.Entities;

namespace Trivia.Application.Tests.Domain;

public class LeaderboardEntryTests
{
    [Fact]
    public void Create_ShouldSetProperties()
    {
        var quizId = Guid.NewGuid();
        var teamId = Guid.NewGuid();

        var entry = new LeaderboardEntry
        {
            Id = Guid.NewGuid(),
            QuizId = quizId,
            TeamId = teamId,
            Score = 100,
            UpdatedAt = DateTime.UtcNow
        };

        entry.QuizId.Should().Be(quizId);
        entry.TeamId.Should().Be(teamId);
        entry.Score.Should().Be(100);
    }

    [Fact]
    public void Score_ShouldBeMutable()
    {
        var entry = new LeaderboardEntry { QuizId = Guid.NewGuid(), TeamId = Guid.NewGuid() };
        entry.Score = 50;
        entry.Score.Should().Be(50);
        entry.Score += 10;
        entry.Score.Should().Be(60);
    }
}
