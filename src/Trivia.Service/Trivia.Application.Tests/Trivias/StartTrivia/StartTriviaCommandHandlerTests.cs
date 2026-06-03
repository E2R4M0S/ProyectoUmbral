using Xunit;
using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Microsoft.Extensions.Logging;
using Trivia.Application.Common.Interfaces;
using Trivia.Application.Trivias.StartTrivia;
using Trivia.Domain.Entities;

namespace Trivia.Application.Tests.Trivias.StartTrivia;

public class StartTriviaCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenQuizExists_ShouldPublishTriviaStarted()
    {
        var repo = Substitute.For<IQuizRepository>();
        var publisher = Substitute.For<IEventPublisher>();
        var logger = Substitute.For<ILogger<StartTriviaCommandHandler>>();
        var handler = new StartTriviaCommandHandler(repo, publisher, logger);

        var quizId = Guid.NewGuid();
        repo.GetByIdAsync(quizId, Arg.Any<CancellationToken>()).Returns(new Quiz { Id = quizId, Title = "Test" });

        var cmd = new StartTriviaCommand(quizId);
        await handler.Handle(cmd, CancellationToken.None);

        await publisher.Received(1).PublishAsync(
            Arg.Is<string>(s => s == "TriviaStarted"),
            Arg.Is<object>(o => o.GetType().GetProperty("QuizId")!.GetValue(o)!.Equals(quizId)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenQuizNotFound_ShouldThrow()
    {
        var repo = Substitute.For<IQuizRepository>();
        var publisher = Substitute.For<IEventPublisher>();
        var logger = Substitute.For<ILogger<StartTriviaCommandHandler>>();
        var handler = new StartTriviaCommandHandler(repo, publisher, logger);

        var quizId = Guid.NewGuid();
        repo.GetByIdAsync(quizId, Arg.Any<CancellationToken>()).Returns((Quiz?)null);

        var cmd = new StartTriviaCommand(quizId);
        Func<Task> act = () => handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*Quiz with id '{quizId}' not found*");
    }
}
