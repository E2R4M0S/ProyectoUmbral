using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Trivia.Application.Common.Interfaces;
using Trivia.Application.Trivias.Questions;
using Xunit;

namespace Trivia.Application.Tests.Questions;

public class AskQuestionCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnQuestionId()
    {
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("realTimeHub").Returns(client);
        var logger = Substitute.For<ILogger<AskQuestionCommandHandler>>();
        var sut = new AskQuestionCommandHandler(factory, logger);

        var command = new AskQuestionCommand(Guid.NewGuid(), "Pregunta?", new[] { "A", "B" }, 30, 0);
        var result = await sut.Handle(command, CancellationToken.None);

        result.Should().NotBeEmpty();
    }
}

public class GetAnswerCountQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnCount()
    {
        var repo = Substitute.For<IParticipantAnswerRepository>();
        repo.GetCountByQuestionAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(5);

        var sut = new GetAnswerCountQueryHandler(repo);
        var result = await sut.Handle(new GetAnswerCountQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().Be(5);
    }
}
