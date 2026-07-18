using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Sessions.Application.Common.Interfaces;
using Sessions.Infrastructure.Notifications;
using Xunit;

namespace Sessions.Service.Sessions.Infrastructure.Tests.Notifications;

public class GameNotifierTests
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GameNotifier> _logger;
    private readonly GameNotifier _notifier;

    public GameNotifierTests()
    {
        _httpClient = new HttpClient(new TestHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        _logger = Substitute.For<ILogger<GameNotifier>>();
        _notifier = new GameNotifier(_httpClient, _logger);
    }

    [Fact]
    public async Task NotifySessionStatusChanged_SendsCorrectPayload()
    {
        var sessionId = Guid.NewGuid();
        var status = "Active";

        await _notifier.NotifySessionStatusChanged(sessionId, status);
    }

    [Fact]
    public async Task NotifyProgressUpdated_SendsCorrectPayload()
    {
        var sessionId = Guid.NewGuid();
        var progressData = new { PercentComplete = 50 };

        await _notifier.NotifyProgressUpdated(sessionId, progressData);
    }

    [Fact]
    public async Task NotifyClueReleased_WithTeamId_SendsCorrectPayload()
    {
        var sessionId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var clueData = new { ClueId = Guid.NewGuid(), Text = "Test clue" };

        await _notifier.NotifyClueReleased(sessionId, teamId, null, clueData);
    }

    [Fact]
    public async Task NotifyClueReleased_WithoutTeamId_SendsCorrectPayload()
    {
        var sessionId = Guid.NewGuid();
        var clueData = new { ClueId = Guid.NewGuid(), Text = "Test clue" };

        await _notifier.NotifyClueReleased(sessionId, null, null, clueData);
    }

    [Fact]
    public async Task NotifyClueReleased_WithUserId_SendsCorrectPayload()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var clueData = new { ClueId = Guid.NewGuid(), Text = "Test clue" };

        await _notifier.NotifyClueReleased(sessionId, null, userId, clueData);
    }

    [Fact]
    public async Task NotifyQuestionClosed_SendsCorrectPayload()
    {
        var sessionId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var correctAnswerId = Guid.NewGuid();
        var correctAnswerText = "42";

        await _notifier.NotifyQuestionClosed(sessionId, questionId, correctAnswerId, correctAnswerText);
    }

    [Fact]
    public async Task NotifySessionRankingUpdatedAsync_PostsToSessionRankingEndpoint()
    {
        // Bug fix: the session-wide ranking must hit the dedicated RealTimeHub endpoint so
        // the trivia-only leaderboard broadcast can't overwrite the participant's score.
        HttpRequestMessage? captured = null;
        // BaseAddress is required by HttpClient.PostAsJsonAsync when posting a relative URL;
        // the existing tests didn't notice because they only assert "doesn't throw".
        var client = new HttpClient(new TestHttpHandler(req =>
        {
            captured = req;
            return new HttpResponseMessage(HttpStatusCode.OK);
        }))
        {
            BaseAddress = new Uri("http://localhost")
        };
        var notifier = new GameNotifier(client, Substitute.For<ILogger<GameNotifier>>());
        var sessionId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var entries = new[]
        {
            new SessionRankingEntry("team", "Alpha", 100, 2, teamId, null, DateTime.UtcNow),
            new SessionRankingEntry("individual", "Bob", 60, 0, null, userId, DateTime.UtcNow.AddSeconds(-1)),
        };

        await notifier.NotifySessionRankingUpdatedAsync(sessionId, entries);

        captured.Should().NotBeNull();
        captured!.RequestUri!.AbsolutePath.Should().Be("/internal/notifications/session-ranking");
        var body = await captured.Content!.ReadFromJsonAsync<SessionRankingBody>();
        body.Should().NotBeNull();
        body!.SessionId.Should().Be(sessionId);
        body.Ranking.Should().HaveCount(2);
        // RB-08: score desc; Alpha (100) before Bob (60) regardless of lastScoreAt.
        body.Ranking[0].TeamName.Should().Be("Alpha");
        body.Ranking[0].Score.Should().Be(100);
        body.Ranking[1].TeamName.Should().Be("Bob");
        body.Ranking[1].Score.Should().Be(60);
    }

    [Fact]
    public async Task NotifySessionRankingUpdatedAsync_WhenHttpFails_DoesNotThrow()
    {
        var badClient = new HttpClient(new TestHttpHandler(_ => throw new HttpRequestException("Network error")));
        var logger = Substitute.For<ILogger<GameNotifier>>();
        var notifier = new GameNotifier(badClient, logger);

        await notifier.Invoking(n => n.NotifySessionRankingUpdatedAsync(
                Guid.NewGuid(),
                new[] { new SessionRankingEntry("team", "Alpha", 10, 1, Guid.NewGuid(), null, DateTime.UtcNow) }))
            .Should().NotThrowAsync();

        logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task NotifySessionStatusChanged_WhenHttpFails_DoesNotThrow()
    {
        var badClient = new HttpClient(new TestHttpHandler(_ => throw new HttpRequestException("Network error")));
        var logger = Substitute.For<ILogger<GameNotifier>>();
        var notifier = new GameNotifier(badClient, logger);

        await notifier.Invoking(n => n.NotifySessionStatusChanged(Guid.NewGuid(), "Active"))
            .Should().NotThrowAsync();

        logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    private record SessionRankingBody(Guid SessionId, List<RankingItem> Ranking);
    private record RankingItem(int Position, string TeamName, int Score, string? UserId);
}

internal class TestHttpHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;
    public TestHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) => _handler = handler;
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        => Task.FromResult(_handler(request));
}