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

public class ParticipantAnswerRepositoryTests
{
    private static TriviaDbContext CreateContext()
    {
        var opts = new DbContextOptionsBuilder<TriviaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new TriviaDbContext(opts);
    }

    [Fact]
    public async Task AddAsync_PersistsAnswer()
    {
        var ctx = CreateContext();
        var repo = new ParticipantAnswerRepository(ctx);

        var pa = new ParticipantAnswer
        {
            Id = Guid.NewGuid(),
            QuizId = Guid.NewGuid(),
            TeamId = Guid.NewGuid(),
            QuestionId = Guid.NewGuid(),
            AnswerId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            IsCorrect = true
        };

        await repo.AddAsync(pa);
        var all = await ctx.ParticipantAnswers.ToListAsync();
        all.Should().ContainSingle().Which.Id.Should().Be(pa.Id);
    }

    [Fact]
    public async Task GetByQuizAsync_ReturnsFiltered()
    {
        var ctx = CreateContext();
        var repo = new ParticipantAnswerRepository(ctx);
        var quizId = Guid.NewGuid();

        ctx.ParticipantAnswers.AddRange(
            new ParticipantAnswer { Id = Guid.NewGuid(), QuizId = quizId, TeamId = Guid.NewGuid(), QuestionId = Guid.NewGuid(), AnswerId = Guid.NewGuid(), Timestamp = DateTime.UtcNow },
            new ParticipantAnswer { Id = Guid.NewGuid(), QuizId = Guid.NewGuid(), TeamId = Guid.NewGuid(), QuestionId = Guid.NewGuid(), AnswerId = Guid.NewGuid(), Timestamp = DateTime.UtcNow });
        await ctx.SaveChangesAsync();

        var results = await repo.GetByQuizAsync(quizId);
        results.Should().ContainSingle();
    }
}
