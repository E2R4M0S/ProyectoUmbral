using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Trivia.Domain.Entities;
using Trivia.Infrastructure;
using Trivia.Infrastructure.Persistence;
using Xunit;

namespace Trivia.Infrastructure.Tests.Persistence;

public class AnswerRepositoryTests
{
    private static TriviaDbContext CreateContext()
    {
        var opts = new DbContextOptionsBuilder<TriviaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new TriviaDbContext(opts);
    }

    [Fact]
    public async Task GetByQuestionIdAsync_ReturnsAnswers()
    {
        var ctx = CreateContext();
        var repo = new AnswerRepository(ctx);
        var qId = Guid.NewGuid();

        ctx.Answers.AddRange(
            new Answer { QuestionId = qId, Text = "A", IsCorrect = true },
            new Answer { QuestionId = qId, Text = "B", IsCorrect = false });
        await ctx.SaveChangesAsync();

        var results = await repo.GetByQuestionIdAsync(qId);
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByQuestionIdAsync_WhenNone_ReturnsEmpty()
    {
        var ctx = CreateContext();
        var repo = new AnswerRepository(ctx);

        var results = await repo.GetByQuestionIdAsync(Guid.NewGuid());
        results.Should().BeEmpty();
    }
}
