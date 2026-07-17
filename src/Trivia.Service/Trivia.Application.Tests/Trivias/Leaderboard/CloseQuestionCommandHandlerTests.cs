using Xunit;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Trivia.Application.Trivias.Leaderboard;

namespace Trivia.Application.Tests.Trivias.Leaderboard;

public class CloseQuestionCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldPostToRealTimeHub()
    {
        var handlerLogger = Substitute.For<ILogger<CloseQuestionCommandHandler>>();
        var httpHandler = new TestHttpMessageHandler(req =>
        {
            Xunit.Assert.Equal("/internal/notifications/question-closed", req.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var client = new HttpClient(httpHandler) { BaseAddress = new Uri("http://localhost:5005") };
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("RealTimeHub").Returns(client);

        var mediator = Substitute.For<IMediator>();
        var handler = new CloseQuestionCommandHandler(handlerLogger, factory, mediator);
        var cmd = new CloseQuestionCommand(SessionId: Guid.NewGuid(), QuestionId: Guid.NewGuid());

        await handler.Handle(cmd, CancellationToken.None);
    }

    [Fact]
    public async Task Handle_WhenHubReturnsError_ShouldNotThrow()
    {
        var handlerLogger = Substitute.For<ILogger<CloseQuestionCommandHandler>>();
        var httpHandler = new TestHttpMessageHandler(req =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var client = new HttpClient(httpHandler) { BaseAddress = new Uri("http://localhost:5005") };
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("RealTimeHub").Returns(client);

        var mediator = Substitute.For<IMediator>();
        var handler = new CloseQuestionCommandHandler(handlerLogger, factory, mediator);
        var cmd = new CloseQuestionCommand(SessionId: Guid.NewGuid(), QuestionId: Guid.NewGuid());

        await handler.Handle(cmd, CancellationToken.None);
    }
}
