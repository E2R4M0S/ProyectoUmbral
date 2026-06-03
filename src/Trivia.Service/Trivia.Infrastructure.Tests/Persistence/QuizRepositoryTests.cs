using System;
using System.Threading.Tasks;
using FluentAssertions;
using Trivia.Domain.Entities;
using Trivia.Infrastructure.Persistence;
using Xunit;

namespace Trivia.Infrastructure.Tests.Persistence;

public class QuizRepositoryTests : DbTestBase
{
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
