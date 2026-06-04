using System.Net;
using System.Net.Http;
using FluentAssertions;
using NSubstitute;
using Trivia.Application.Trivias.Answers;
using Trivia.Application.Trivias.Questions;
using Trivia.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Xunit;
using Trivia.Domain.Entities;

namespace Trivia.Application.Tests.Trivias.Answers;

public class SubmitAnswerCommandHandlerScoringTests
{
    private static HttpClient CreateOkClient()
    {
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        return new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
    }

    [Fact]
    public async Task CorrectAnswer_FirstPosition_Gets40Points()
    {
        var questionId = Guid.NewGuid();
        AskQuestionCommandHandler.CorrectAnswers[questionId] = 0;
        AskQuestionCommandHandler.CorrectAnswerTimestamps[questionId] = new List<DateTime>();

        var leaderboardRepo = Substitute.For<ILeaderboardRepository>();
        leaderboardRepo.GetByTeamAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((LeaderboardEntry?)null);
        var answerRepo = Substitute.For<IParticipantAnswerRepository>();
        var publisher = Substitute.For<IEventPublisher>();
        var logger = Substitute.For<ILogger<SubmitAnswerCommandHandler>>();
        var httpFactory = Substitute.For<IHttpClientFactory>();
        httpFactory.CreateClient("realTimeHub").Returns(CreateOkClient());

        var sut = new SubmitAnswerCommandHandler(publisher, logger, httpFactory, answerRepo, leaderboardRepo);
        var cmd = new SubmitAnswerCommand(Guid.NewGuid(), Guid.NewGuid(), "Team A", questionId,
            Guid.Parse("00000000-0000-0000-0000-000000000000"), DateTime.UtcNow, DateTime.UtcNow, 30);

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.IsCorrect.Should().BeTrue();
        result.PointsAwarded.Should().Be(40); // 10 base + 30 first position
        result.Position.Should().Be(1);
    }

    [Fact]
    public async Task IncorrectAnswer_GetsZeroPoints()
    {
        var questionId = Guid.NewGuid();
        AskQuestionCommandHandler.CorrectAnswers[questionId] = 0;
        AskQuestionCommandHandler.CorrectAnswerTimestamps[questionId] = new List<DateTime>();

        var publisher = Substitute.For<IEventPublisher>();
        var logger = Substitute.For<ILogger<SubmitAnswerCommandHandler>>();
        var httpFactory = Substitute.For<IHttpClientFactory>();
        httpFactory.CreateClient("realTimeHub").Returns(CreateOkClient());

        var sut = new SubmitAnswerCommandHandler(publisher, logger, httpFactory);
        var cmd = new SubmitAnswerCommand(Guid.NewGuid(), Guid.NewGuid(), "Team A", questionId,
            Guid.Parse("00000000-0000-0000-0000-000000000001"), DateTime.UtcNow, DateTime.UtcNow, 30);

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.IsCorrect.Should().BeFalse();
        result.PointsAwarded.Should().Be(0);
    }

    [Fact]
    public async Task WithoutLeaderboardRepo_StillPublishesEvent()
    {
        var publisher = Substitute.For<IEventPublisher>();
        var logger = Substitute.For<ILogger<SubmitAnswerCommandHandler>>();
        var httpFactory = Substitute.For<IHttpClientFactory>();
        var sut = new SubmitAnswerCommandHandler(publisher, logger, httpFactory);
        var cmd = new SubmitAnswerCommand(Guid.NewGuid(), Guid.NewGuid(), "Team", Guid.NewGuid(),
            Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow, 30);

        await sut.Handle(cmd, CancellationToken.None);

        await publisher.Received(1).PublishAsync(
            Arg.Is<string>(s => s.Contains("TriviaAnswerSubmitted")),
            Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExistingLeaderboardEntry_AddsPoints()
    {
        var questionId = Guid.NewGuid();
        AskQuestionCommandHandler.CorrectAnswers[questionId] = 0;
        AskQuestionCommandHandler.CorrectAnswerTimestamps[questionId] = new List<DateTime>();

        var leaderboardRepo = Substitute.For<ILeaderboardRepository>();
        var existingEntry = new LeaderboardEntry { QuizId = Guid.NewGuid(), TeamId = Guid.NewGuid(), TeamName = "T", Score = 50 };
        leaderboardRepo.GetByTeamAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(existingEntry);
        var answerRepo = Substitute.For<IParticipantAnswerRepository>();
        var publisher = Substitute.For<IEventPublisher>();
        var logger = Substitute.For<ILogger<SubmitAnswerCommandHandler>>();
        var httpFactory = Substitute.For<IHttpClientFactory>();
        httpFactory.CreateClient("realTimeHub").Returns(CreateOkClient());

        var sut = new SubmitAnswerCommandHandler(publisher, logger, httpFactory, answerRepo, leaderboardRepo);
        var cmd = new SubmitAnswerCommand(Guid.NewGuid(), Guid.NewGuid(), "Team A", questionId,
            Guid.Parse("00000000-0000-0000-0000-000000000000"), DateTime.UtcNow, DateTime.UtcNow, 30);

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.PointsAwarded.Should().Be(40);
        existingEntry.Score.Should().Be(90); // 50 + 40
    }
}
