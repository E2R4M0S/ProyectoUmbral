using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Trivia.Infrastructure;
using Trivia.Infrastructure.Persistence;
using Trivia.Domain.Entities;
using Xunit;

namespace Trivia.Infrastructure.Tests.Persistence;

public class LeaderboardRepositoryTests
{
    private TriviaDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TriviaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TriviaDbContext(options);
    }

    [Fact]
    public async Task AddOrUpdateAsync_AddsNewEntry()
    {
        var ctx = CreateContext();
        var repo = new LeaderboardRepository(ctx);

        var entry = new LeaderboardEntry { QuizId = Guid.NewGuid(), TeamId = Guid.NewGuid(), Score = 10 };
        await repo.AddOrUpdateAsync(entry);

        var list = await repo.GetByQuizAsync(entry.QuizId);
        list.Should().ContainSingle().Which.Score.Should().Be(10);
    }

    [Fact]
    public async Task AddOrUpdateAsync_UpdatesExisting()
    {
        var ctx = CreateContext();
        var repo = new LeaderboardRepository(ctx);

        var quiz = Guid.NewGuid();
        var team = Guid.NewGuid();
        var entry = new LeaderboardEntry { QuizId = quiz, TeamId = team, Score = 10 };
        await repo.AddOrUpdateAsync(entry);

        entry.Score = 25;
        await repo.AddOrUpdateAsync(entry);

        var got = await repo.GetByTeamAsync(quiz, team);
        got.Should().NotBeNull();
        got!.Score.Should().Be(25);
    }
}
