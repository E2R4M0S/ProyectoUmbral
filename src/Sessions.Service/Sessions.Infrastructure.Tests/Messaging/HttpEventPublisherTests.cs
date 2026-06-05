using System.Net;
using System.Net.Http;
using System.Threading;
using FluentAssertions;
using Sessions.Application.Common.Interfaces;
using Sessions.Infrastructure.Messaging;
using Xunit;

namespace Sessions.Service.Sessions.Infrastructure.Tests.Messaging;

public class HttpEventPublisherTests
{
    [Fact]
    public async Task PublishAsync_SessionStarted_MapsToCorrectEndpoint()
    {
        var httpHandler = new TestHttpHandler(req =>
        {
            req.Method.Should().Be(HttpMethod.Post);
            req.RequestUri!.AbsolutePath.Should().Be("/internal/notifications/session-status");
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var client = new HttpClient(httpHandler) { BaseAddress = new Uri("http://localhost:5005") };
        var publisher = new HttpEventPublisher(client);

        await publisher.PublishAsync("SessionStarted", new { SessionId = Guid.NewGuid() }, CancellationToken.None);
    }

    [Fact]
    public async Task PublishAsync_ProgressUpdated_MapsToCorrectEndpoint()
    {
        var httpHandler = new TestHttpHandler(req =>
        {
            req.Method.Should().Be(HttpMethod.Post);
            req.RequestUri!.AbsolutePath.Should().Be("/internal/notifications/progress");
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var client = new HttpClient(httpHandler) { BaseAddress = new Uri("http://localhost:5005") };
        var publisher = new HttpEventPublisher(client);

        await publisher.PublishAsync("ProgressUpdated", new { Percent = 50 }, CancellationToken.None);
    }

    [Fact]
    public async Task PublishAsync_ClueReleased_MapsToCorrectEndpoint()
    {
        var httpHandler = new TestHttpHandler(req =>
        {
            req.Method.Should().Be(HttpMethod.Post);
            req.RequestUri!.AbsolutePath.Should().Be("/internal/notifications/clue-released");
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var client = new HttpClient(httpHandler) { BaseAddress = new Uri("http://localhost:5005") };
        var publisher = new HttpEventPublisher(client);

        await publisher.PublishAsync("ClueReleased", new { ClueId = Guid.NewGuid() }, CancellationToken.None);
    }

    [Fact]
    public async Task PublishAsync_UnknownEvent_MapsToEventsEndpoint()
    {
        var httpHandler = new TestHttpHandler(req =>
        {
            req.Method.Should().Be(HttpMethod.Post);
            req.RequestUri!.AbsolutePath.Should().Be("/internal/events/CustomEvent");
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var client = new HttpClient(httpHandler) { BaseAddress = new Uri("http://localhost:5005") };
        var publisher = new HttpEventPublisher(client);

        await publisher.PublishAsync("CustomEvent", new { Data = "test" }, CancellationToken.None);
    }

    [Fact]
    public async Task PublishAsync_WhenHubFails_DoesNotThrow()
    {
        var httpHandler = new TestHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var client = new HttpClient(httpHandler) { BaseAddress = new Uri("http://localhost:5005") };
        var publisher = new HttpEventPublisher(client);

        await publisher.Invoking(p => p.PublishAsync("SessionStarted", new { }, CancellationToken.None))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishAsync_WhenNetworkFails_DoesNotThrow()
    {
        var httpHandler = new TestHttpHandler(_ => throw new HttpRequestException("Network error"));
        var client = new HttpClient(httpHandler) { BaseAddress = new Uri("http://localhost:5005") };
        var publisher = new HttpEventPublisher(client);

        await publisher.Invoking(p => p.PublishAsync("SessionStarted", new { }, CancellationToken.None))
            .Should().NotThrowAsync();
    }
}

internal class TestHttpHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;
    public TestHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) => _handler = handler;
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        => Task.FromResult(_handler(request));
}