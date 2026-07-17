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
    public async Task Handle_ShouldReturnCountOfTeamsThatAnsweredTheQuestion()
    {
        // GetAnswerCountQueryHandler counts unique teams from AskQuestionCommandHandler's
        // in-memory TeamAnswers dictionary — it no longer reads from a repository.
        var questionId = Guid.NewGuid();
        var otherQuestionId = Guid.NewGuid();
        AskQuestionCommandHandler.TeamAnswers[(questionId, Guid.NewGuid())] = 0;
        AskQuestionCommandHandler.TeamAnswers[(questionId, Guid.NewGuid())] = 1;
        AskQuestionCommandHandler.TeamAnswers[(otherQuestionId, Guid.NewGuid())] = 0;

        var sut = new GetAnswerCountQueryHandler();
        var result = await sut.Handle(new GetAnswerCountQuery(questionId), CancellationToken.None);

        result.Should().Be(2);
    }
}
