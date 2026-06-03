using Xunit;
using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Trivia.Application.Common.Interfaces;
using Trivia.Application.Trivias.Leaderboard;
using Trivia.Domain.Entities;

namespace Trivia.Application.Tests.Trivias.Leaderboard;

public class UpdateLeaderboardCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenEntryExists_ShouldUpdateScore()
    {
        var repo = Substitute.For<ILeaderboardRepository>();
        var publisher = Substitute.For<IEventPublisher>();
        var handler = new UpdateLeaderboardCommandHandler(repo, publisher);

        var quizId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var existing = new LeaderboardEntry { QuizId = quizId, TeamId = teamId, Score = 50 };

        repo.GetByTeamAsync(quizId, teamId, Arg.Any<CancellationToken>()).Returns(existing);

        var cmd = new UpdateLeaderboardCommand(quizId, teamId, 10);
        await handler.Handle(cmd, CancellationToken.None);

        Xunit.Assert.Equal(60, existing.Score);
        await repo.Received(1).AddOrUpdateAsync(existing, Arg.Any<CancellationToken>());
        await publisher.Received(1).PublishAsync("LeaderboardUpdated", Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenEntryNotExists_ShouldCreateNew()
    {
        var repo = Substitute.For<ILeaderboardRepository>();
        var publisher = Substitute.For<IEventPublisher>();
        var handler = new UpdateLeaderboardCommandHandler(repo, publisher);

        var quizId = Guid.NewGuid();
        var teamId = Guid.NewGuid();

        repo.GetByTeamAsync(quizId, teamId, Arg.Any<CancellationToken>()).Returns((LeaderboardEntry?)null);

        var cmd = new UpdateLeaderboardCommand(quizId, teamId, 10);
        await handler.Handle(cmd, CancellationToken.None);

        await repo.Received(1).AddOrUpdateAsync(
            Arg.Is<LeaderboardEntry>(e => e.Score == 10 && e.TeamId == teamId),
            Arg.Any<CancellationToken>());
    }
}
