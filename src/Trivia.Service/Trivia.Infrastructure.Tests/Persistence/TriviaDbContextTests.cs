using System;
using System.Linq;
using FluentAssertions;
using Trivia.Domain.Entities;
using Xunit;

namespace Trivia.Infrastructure.Tests.Persistence;

public class TriviaDbContextTests : DbTestBase
{
    [Fact]
    public void CanCreateDatabase_WithPostgres()
    {
        using var ctx = CreateContext();
        ctx.Questions.Should().NotBeNull();
        ctx.Set<Quiz>().Should().NotBeNull();
        ctx.Answers.Should().NotBeNull();
        ctx.ParticipantAnswers.Should().NotBeNull();
        ctx.Set<LeaderboardEntry>().Should().NotBeNull();
    }

    [Fact]
    public void CanSeedAndQueryQuestions()
    {
        using var ctx = CreateContext();
        ctx.Questions.Add(new Question { Id = Guid.NewGuid(), QuizId = Guid.NewGuid(), TimeLimitSeconds = 30 });
        ctx.SaveChanges();

        ctx.Questions.Count().Should().Be(1);
    }

    [Fact]
    public void CanSeedAndQueryQuizzes()
    {
        using var ctx = CreateContext();
        ctx.Set<Quiz>().Add(new Quiz { Id = Guid.NewGuid(), Title = "Test Quiz" });
        ctx.SaveChanges();

        ctx.Set<Quiz>().Count().Should().Be(1);
    }
}
