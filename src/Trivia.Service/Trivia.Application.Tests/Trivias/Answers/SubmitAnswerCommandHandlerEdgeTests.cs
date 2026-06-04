using Xunit;
using System;
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
        var handler = new SubmitAnswerCommandHandler(publisher, logger, null, null, null);

        var cmd = new SubmitAnswerCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);

        await handler.Invoking(h => h.Handle(cmd, CancellationToken.None))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task Handle_WhenAnswerRepoFails_ShouldThrow()
    {
        var publisher = Substitute.For<IEventPublisher>();
        var logger = Substitute.For<ILogger<SubmitAnswerCommandHandler>>();

        var answerRepo = Substitute.For<IParticipantAnswerRepository>();
        answerRepo.When(x => x.AddAsync(Arg.Any<ParticipantAnswer>(), Arg.Any<CancellationToken>()))
            .Throw(new Exception("DB error"));

        var handler = new SubmitAnswerCommandHandler(publisher, logger, answerRepo, null, null);

        var cmd = new SubmitAnswerCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);

        await handler.Invoking(h => h.Handle(cmd, CancellationToken.None))
            .Should().ThrowAsync<Exception>();
    }
}
