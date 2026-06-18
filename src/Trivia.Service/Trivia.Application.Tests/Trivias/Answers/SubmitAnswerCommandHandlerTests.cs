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
    public async Task Handle_PublishesEvent()
    {
        var publisher = Substitute.For<IEventPublisher>();
        var logger = Substitute.For<ILogger<SubmitAnswerCommandHandler>>();
        var httpFactory = Substitute.For<IHttpClientFactory>();
        httpFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient());
        var scoringStrategy = Substitute.For<IScoringStrategy>();

        var handler = new SubmitAnswerCommandHandler(publisher, logger, httpFactory, scoringStrategy);

        var cmd = new SubmitAnswerCommand(Guid.NewGuid(), Guid.NewGuid(), "Test", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow, 30);

        await handler.Handle(cmd, CancellationToken.None);

        await publisher.Received().PublishAsync(Arg.Is<string>(s => s.Contains("TriviaAnswerSubmitted")), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }
}