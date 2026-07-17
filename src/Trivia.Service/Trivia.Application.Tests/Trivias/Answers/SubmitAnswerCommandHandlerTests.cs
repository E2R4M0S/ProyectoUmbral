using System;
using System.Net;
using System.Net.Http;
using System.Text;
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

    private static HttpClient SessionStatusClient(string status)
    {
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent($"{{\"status\":\"{status}\"}}", Encoding.UTF8, "application/json")
        });
        return new HttpClient(handler) { BaseAddress = new Uri("http://sessions.service") };
    }

    // RB-03: no debe aceptar respuestas si la sesión está pausada, finalizada o cancelada.
    [Theory]
    [InlineData("Paused")]
    [InlineData("Finished")]
    [InlineData("Cancelled")]
    public async Task Handle_WhenSessionIsBlocked_ShouldRejectAndNotPublish(string blockedStatus)
    {
        var publisher = Substitute.For<IEventPublisher>();
        var logger = Substitute.For<ILogger<SubmitAnswerCommandHandler>>();
        var httpFactory = Substitute.For<IHttpClientFactory>();
        httpFactory.CreateClient("sessionsService").Returns(SessionStatusClient(blockedStatus));
        httpFactory.CreateClient("realTimeHub").Returns(new HttpClient());
        var scoringStrategy = Substitute.For<IScoringStrategy>();

        var handler = new SubmitAnswerCommandHandler(publisher, logger, httpFactory, scoringStrategy);
        var cmd = new SubmitAnswerCommand(Guid.NewGuid(), Guid.NewGuid(), "Test", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow, 30);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Rejected.Should().BeTrue();
        result.RejectReason.Should().Contain(blockedStatus);
        await publisher.DidNotReceive().PublishAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSessionIsActive_ShouldNotReject()
    {
        var publisher = Substitute.For<IEventPublisher>();
        var logger = Substitute.For<ILogger<SubmitAnswerCommandHandler>>();
        var httpFactory = Substitute.For<IHttpClientFactory>();
        httpFactory.CreateClient("sessionsService").Returns(SessionStatusClient("Active"));
        httpFactory.CreateClient("realTimeHub").Returns(new HttpClient());
        var scoringStrategy = Substitute.For<IScoringStrategy>();

        var handler = new SubmitAnswerCommandHandler(publisher, logger, httpFactory, scoringStrategy);
        var cmd = new SubmitAnswerCommand(Guid.NewGuid(), Guid.NewGuid(), "Test", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow, 30);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Rejected.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenSessionsServiceIsUnreachable_ShouldFailOpenAndProcessTheAnswer()
    {
        var publisher = Substitute.For<IEventPublisher>();
        var logger = Substitute.For<ILogger<SubmitAnswerCommandHandler>>();
        var httpFactory = Substitute.For<IHttpClientFactory>();
        // No stub for "sessionsService" — CreateClient returns null, forcing the internal
        // try/catch to swallow the failure and allow the answer through.
        httpFactory.CreateClient("realTimeHub").Returns(new HttpClient());
        var scoringStrategy = Substitute.For<IScoringStrategy>();

        var handler = new SubmitAnswerCommandHandler(publisher, logger, httpFactory, scoringStrategy);
        var cmd = new SubmitAnswerCommand(Guid.NewGuid(), Guid.NewGuid(), "Test", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow, 30);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Rejected.Should().BeFalse();
    }
}