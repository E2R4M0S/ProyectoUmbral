using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Trivia.Domain.Entities;
using Trivia.Infrastructure;
using Trivia.Infrastructure.Persistence;
using Xunit;

namespace Trivia.Infrastructure.Tests.Persistence;

public class QuizRepositoryTests
{
    private static TriviaDbContext CreateContext()
    {
        var opts = new DbContextOptionsBuilder<TriviaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new TriviaDbContext(opts);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsQuiz()
    {
        var ctx = CreateContext();
        var repo = new QuizRepository(ctx);
        var id = Guid.NewGuid();

        ctx.Set<Quiz>().Add(new Quiz { Id = id, Title = "Test" });
        await ctx.SaveChangesAsync();

        var quiz = await repo.GetByIdAsync(id);
        quiz.Should().NotBeNull();
        quiz!.Title.Should().Be("Test");
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        var ctx = CreateContext();
        var repo = new QuizRepository(ctx);

        var quiz = await repo.GetByIdAsync(Guid.NewGuid());
        quiz.Should().BeNull();
    }
}
