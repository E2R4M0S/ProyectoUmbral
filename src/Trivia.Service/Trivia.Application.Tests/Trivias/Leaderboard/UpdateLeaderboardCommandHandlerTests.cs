using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Trivia.Application.Trivias.Leaderboard;
using Trivia.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Trivia.Application.Tests.Trivias.Leaderboard;

public class UpdateLeaderboardCommandHandlerTests
{
    [Fact]
    public async Task Handle_CreatesOrUpdatesEntryAndPublishesSnapshot()
    {
        var repo = Substitute.For<ILeaderboardRepository>();
        var publisher = Substitute.For<IEventPublisher>();

        repo.GetByTeamAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Entities.LeaderboardEntry?)null);

        repo.GetByQuizAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<Domain.Entities.LeaderboardEntry>());

        var handler = new UpdateLeaderboardCommandHandler(repo, publisher);
        var cmd = new UpdateLeaderboardCommand(Guid.NewGuid(), Guid.NewGuid(), 10);

        await handler.Handle(cmd, CancellationToken.None);

        await repo.Received(1).AddOrUpdateAsync(Arg.Any<Domain.Entities.LeaderboardEntry>(), Arg.Any<CancellationToken>());
        await publisher.Received(1).PublishAsync("LeaderboardUpdated", Arg.Any<object>(), Arg.Any<CancellationToken>());
    }
}
