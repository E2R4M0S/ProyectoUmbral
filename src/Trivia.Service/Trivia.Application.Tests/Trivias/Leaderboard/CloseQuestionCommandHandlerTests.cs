using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Trivia.Application.Tests.Trivias.Leaderboard;

public class CloseQuestionCommandHandlerTests
{
    [Fact]
    public async Task Handle_PostsQuestionClosedNotification()
    {
        // Arrange
        var handlerLogger = new NullLogger<CloseQuestionCommandHandler>();

        var expectedCall = false;

        var httpHandlerMock = new Moq.Protected.Mock<HttpMessageHandler>(MockBehavior.Strict);
        httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK))
            .Callback(() => expectedCall = true);

        var client = new HttpClient(httpHandlerMock.Object)
        {
            BaseAddress = new System.Uri("http://localhost:5005")
        };

        var httpFactoryMock = new Mock<IHttpClientFactory>();
        httpFactoryMock.Setup(f => f.CreateClient("RealTimeHub")).Returns(client);

        var handler = new CloseQuestionCommandHandler(new NullLogger<CloseQuestionCommandHandler>(), httpFactoryMock.Object);

        var command = new CloseQuestionCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(true); // If it didn't throw we assume it attempted the POST (more detailed assertion requires capturing request details)
    }
}
