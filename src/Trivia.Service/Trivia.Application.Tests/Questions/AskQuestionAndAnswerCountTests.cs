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
        var questionRepo = Substitute.For<IQuestionRepository>();
        var logger = Substitute.For<ILogger<AskQuestionCommandHandler>>();
        var sut = new AskQuestionCommandHandler(factory, questionRepo, logger);

        var command = new AskQuestionCommand(Guid.NewGuid(), "Pregunta?", new[] { "A", "B" }, 30, 0);
        var result = await sut.Handle(command, CancellationToken.None);

        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_WithoutPersistedQuestionId_ShouldNotMarkAnythingReleased()
    {
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("realTimeHub").Returns(client);
        var questionRepo = Substitute.For<IQuestionRepository>();
        var logger = Substitute.For<ILogger<AskQuestionCommandHandler>>();
        var sut = new AskQuestionCommandHandler(factory, questionRepo, logger);

        var command = new AskQuestionCommand(Guid.NewGuid(), "Pregunta?", new[] { "A", "B" }, 30, 0);
        await sut.Handle(command, CancellationToken.None);

        await questionRepo.DidNotReceive().MarkReleasedAsync(Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithPersistedQuestionId_ShouldMarkItReleasedAndReuseTheSameId()
    {
        // HU-27/HU-29: a round asked from the quiz bank must persist ReleasedAt and
        // keep using the quiz bank's question id downstream (UserAnswers/TeamAnswers keys).
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("realTimeHub").Returns(client);
        var questionRepo = Substitute.For<IQuestionRepository>();
        var logger = Substitute.For<ILogger<AskQuestionCommandHandler>>();
        var sut = new AskQuestionCommandHandler(factory, questionRepo, logger);

        var persistedQuestionId = Guid.NewGuid();
        var command = new AskQuestionCommand(Guid.NewGuid(), "Pregunta?", new[] { "A", "B" }, 30, 0, persistedQuestionId);
        var result = await sut.Handle(command, CancellationToken.None);

        result.Should().Be(persistedQuestionId);
        await questionRepo.Received(1).MarkReleasedAsync(persistedQuestionId, Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenMarkReleasedThrows_ShouldStillAskTheQuestion()
    {
        // A DB hiccup while persisting ReleasedAt must not block live gameplay.
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("realTimeHub").Returns(client);
        var questionRepo = Substitute.For<IQuestionRepository>();
        questionRepo.MarkReleasedAsync(Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("DB unavailable"));
        var logger = Substitute.For<ILogger<AskQuestionCommandHandler>>();
        var sut = new AskQuestionCommandHandler(factory, questionRepo, logger);

        var persistedQuestionId = Guid.NewGuid();
        var command = new AskQuestionCommand(Guid.NewGuid(), "Pregunta?", new[] { "A", "B" }, 30, 0, persistedQuestionId);
        var result = await sut.Handle(command, CancellationToken.None);

        result.Should().Be(persistedQuestionId);
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
