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
}

internal class TestHttpHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;
    public TestHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) => _handler = handler;
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        => Task.FromResult(_handler(request));
}