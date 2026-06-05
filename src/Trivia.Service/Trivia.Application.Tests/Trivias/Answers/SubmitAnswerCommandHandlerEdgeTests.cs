using Xunit;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Microsoft.Extensions.Logging;
using Trivia.Application.Common.Interfaces;
using Trivia.Domain.Entities;
using Trivia.Application.Trivias.Answers;

namespace Trivia.Application.Tests.Trivias.Answers;

public class SubmitAnswerCommandHandlerEdgeTests
{
    [Fact]
    public async Task Handle_WithNullRepos_ShouldPublishWithoutCrash()
    {
        var publisher = Substitute.For<IEventPublisher>();
        var logger = Substitute.For<ILogger<SubmitAnswerCommandHandler>>();
        var httpFactory = Substitute.For<IHttpClientFactory>();
        httpFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient());
        var scoringStrategy = Substitute.For<IScoringStrategy>();
        var handler = new SubmitAnswerCommandHandler(publisher, logger, httpFactory, scoringStrategy);

        var cmd = new SubmitAnswerCommand(Guid.NewGuid(), Guid.NewGuid(), "Test", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow, 30);

        await handler.Invoking(h => h.Handle(cmd, CancellationToken.None))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task Handle_WhenAnswerRepoFails_ShouldThrow()
    {
        var publisher = Substitute.For<IEventPublisher>();
        var logger = Substitute.For<ILogger<SubmitAnswerCommandHandler>>();
        var httpFactory = Substitute.For<IHttpClientFactory>();
        httpFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient());

        var answerRepo = Substitute.For<IParticipantAnswerRepository>();
        answerRepo.When(x => x.AddAsync(Arg.Any<ParticipantAnswer>(), Arg.Any<CancellationToken>()))
            .Throw(new Exception("DB error"));

        var scoringStrategy = Substitute.For<IScoringStrategy>();
        var handler = new SubmitAnswerCommandHandler(publisher, logger, httpFactory, scoringStrategy, answerRepo);

        var cmd = new SubmitAnswerCommand(Guid.NewGuid(), Guid.NewGuid(), "Test", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow, 30);

        await handler.Invoking(h => h.Handle(cmd, CancellationToken.None))
            .Should().ThrowAsync<Exception>();
    }
}