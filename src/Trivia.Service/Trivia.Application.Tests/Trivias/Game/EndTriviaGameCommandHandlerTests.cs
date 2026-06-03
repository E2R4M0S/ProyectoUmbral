using Xunit;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Trivia.Application.Trivias.Game;

namespace Trivia.Application.Tests.Trivias.Game;

public class EndTriviaGameCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenSessionsApiReturnsSuccess_ShouldComplete()
    {
        var handlerLogger = Substitute.For<ILogger<EndTriviaGameCommandHandler>>();
        var httpHandler = new TestHttpMessageHandler(req =>
            new HttpResponseMessage(HttpStatusCode.OK));

        var client = new HttpClient(httpHandler) { BaseAddress = new Uri("http://localhost:5002") };
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("sessionsService").Returns(client);

        var handler = new EndTriviaGameCommandHandler(factory, handlerLogger);
        var cmd = new EndTriviaGameCommand(SessionId: Guid.NewGuid());

        await handler.Handle(cmd, CancellationToken.None);
    }

    [Fact]
    public async Task Handle_WhenSessionsApiReturnsError_ShouldThrow()
    {
        var handlerLogger = Substitute.For<ILogger<EndTriviaGameCommandHandler>>();
        var httpHandler = new TestHttpMessageHandler(req =>
            new HttpResponseMessage(HttpStatusCode.NotFound));

        var client = new HttpClient(httpHandler) { BaseAddress = new Uri("http://localhost:5002") };
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("sessionsService").Returns(client);

        var handler = new EndTriviaGameCommandHandler(factory, handlerLogger);
        var cmd = new EndTriviaGameCommand(SessionId: Guid.NewGuid());

        Func<Task> act = () => handler.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Failed to end trivia game*");
    }
}
