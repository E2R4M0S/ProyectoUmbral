using Xunit;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Trivia.Application.Trivias.Ranking;

namespace Trivia.Application.Tests.Trivias.Ranking;

public class UpdateRankingCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnRankingAndNotifyHub()
    {
        var handlerLogger = Substitute.For<ILogger<UpdateRankingCommandHandler>>();
        var httpHandler = new TestHttpMessageHandler(req =>
        {
            Xunit.Assert.Equal("/internal/notifications/ranking-updated", req.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var client = new HttpClient(httpHandler) { BaseAddress = new Uri("http://localhost:5005") };
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("realTimeHub").Returns(client);

        var handler = new UpdateRankingCommandHandler(factory, handlerLogger);
        var cmd = new UpdateRankingCommand(SessionId: Guid.NewGuid());

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().HaveCount(4);
        result[0].TeamName.Should().Be("Equipo A");
        result[0].Score.Should().Be(120);
    }

    [Fact]
    public async Task Handle_WhenHubReturnsError_ShouldNotThrow()
    {
        var handlerLogger = Substitute.For<ILogger<UpdateRankingCommandHandler>>();
        var httpHandler = new TestHttpMessageHandler(req =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var client = new HttpClient(httpHandler) { BaseAddress = new Uri("http://localhost:5005") };
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("realTimeHub").Returns(client);

        var handler = new UpdateRankingCommandHandler(factory, handlerLogger);
        var cmd = new UpdateRankingCommand(SessionId: Guid.NewGuid());

        var result = await handler.Handle(cmd, CancellationToken.None);
        result.Should().NotBeNull();
    }
}
