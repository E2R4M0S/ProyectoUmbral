using System;
using System.Linq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Trivia.Domain.Entities;
using Trivia.Infrastructure;
using Xunit;

namespace Trivia.Infrastructure.Tests.Persistence;

public class TriviaDbContextTests
{
    [Fact]
    public void CanCreateDatabase_UsingInMemory()
    {
        var opts = new DbContextOptionsBuilder<TriviaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

using var ctx = new TriviaDbContext(opts);
ctx.Database.EnsureCreated();
ctx.Questions.Should().NotBeNull();
ctx.Set<Quiz>().Should().NotBeNull();
ctx.Answers.Should().NotBeNull();
ctx.ParticipantAnswers.Should().NotBeNull();
ctx.Set<LeaderboardEntry>().Should().NotBeNull();
    }

    [Fact]
    public void CanSeedAndQueryQuestions()
    {
        var opts = new DbContextOptionsBuilder<TriviaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

        using var ctx = new TriviaDbContext(opts);
        ctx.Questions.Add(new Question { Id = Guid.NewGuid(), QuizId = Guid.NewGuid(), TimeLimitSeconds = 30 });
        ctx.SaveChanges();

        ctx.Questions.Count().Should().Be(1);
    }

    [Fact]
    public void CanSeedAndQueryQuizzes()
    {
        var opts = new DbContextOptionsBuilder<TriviaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

using var ctx = new TriviaDbContext(opts);
ctx.Set<Quiz>().Add(new Quiz { Id = Guid.NewGuid(), Title = "Test Quiz" });
ctx.SaveChanges();

ctx.Set<Quiz>().Count().Should().Be(1);
    }
}
