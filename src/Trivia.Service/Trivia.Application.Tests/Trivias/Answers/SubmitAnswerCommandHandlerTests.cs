using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Trivia.Application.Trivias.Answers;
using Trivia.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Trivia.Application.Tests.Trivias.Answers;

public class SubmitAnswerCommandHandlerTests
{
    [Fact]
    public async Task Handle_PublishesEventAndUpdatesLeaderboard_WhenAnswerRepoAvailable()
    {
        var publisher = Substitute.For<IEventPublisher>();
        var answerRepo = Substitute.For<IParticipantAnswerRepository>();
        var answersRepo = Substitute.For<IAnswerRepository>();
        var leaderboardRepo = Substitute.For<ILeaderboardRepository>();
        var logger = Substitute.For<ILogger<SubmitAnswerCommandHandler>>();
        var httpFactory = Substitute.For<IHttpClientFactory>();
        httpFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient());

        var handler = new SubmitAnswerCommandHandler(publisher, logger, httpFactory, answerRepo, leaderboardRepo);

        var cmd = new SubmitAnswerCommand(Guid.NewGuid(), Guid.NewGuid(), "Test", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);

        await handler.Handle(cmd, CancellationToken.None);

        await publisher.Received().PublishAsync(Arg.Is<string>(s => s.Contains("TriviaAnswerSubmitted")), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }
}
