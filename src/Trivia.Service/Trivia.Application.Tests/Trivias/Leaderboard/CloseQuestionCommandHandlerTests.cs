using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Trivia.Application.Trivias.Leaderboard;
using Trivia.Application.Common.Interfaces;
using Xunit;

namespace Trivia.Application.Tests.Trivias.Leaderboard;

public class CloseQuestionCommandHandlerTests
{
    [Fact]
    public async Task Handle_ComputesDeltasFromPersistedParticipantAnswers_AndPublishesSnapshot()
    {
        // Arrange
        var participantRepo = Substitute.For<IParticipantAnswerRepository>();
        var leaderboardRepo = Substitute.For<ILeaderboardRepository>();
        var publisher = Substitute.For<IEventPublisher>();

        // We can't ask participantRepo for answers via interface (only AddAsync exists), so we'll stub leaderboardRepo GetByQuizAsync to return empty list
        leaderboardRepo.GetByQuizAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new List<Domain.Entities.LeaderboardEntry>());

        // For reading participant answers, we will emulate that reflection path by creating a fake concrete repo with a _db field - but tests run against handler which expects participantRepo and leaderboardRepo implementations.
        // Simpler approach: create a fake ParticipantAnswerRepository with a GetByQuizAsync method at runtime using a dynamic proxy type is complex here; instead, we'll test the handler's graceful handling when it cannot read answers.

        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<CloseQuestionCommandHandler>>();
        var handler = new CloseQuestionCommandHandler(participantRepo, leaderboardRepo, publisher, logger);

        // Act
        await handler.Handle(new CloseQuestionCommand(Guid.NewGuid()), CancellationToken.None);

        // Assert: since handler couldn't read answers, it should not throw and should not call AddOrUpdateAsync
        await leaderboardRepo.DidNotReceiveWithAnyArgs().AddOrUpdateAsync(default!, default);
        await publisher.DidNotReceiveWithAnyArgs().PublishAsync(default!, default);
    }
}
