using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Trivia.Infrastructure.Messaging;
using Xunit;

namespace Trivia.Infrastructure.Tests.Messaging;

public class HttpEventPublisherTests
{
    [Fact]
    public async Task PublishAsync_SendsPostToRealTimeHub()
    {
        var httpHandler = new TestHttpHandler(req =>
        {
            req.Method.Should().Be(HttpMethod.Post);
            req.RequestUri!.AbsolutePath.Should().Be("/internal/notifications/event");
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var client = new HttpClient(httpHandler) { BaseAddress = new Uri("http://localhost:5005") };
        var logger = Substitute.For<ILogger<HttpEventPublisher>>();
        var publisher = new HttpEventPublisher(client, logger);

        await publisher.PublishAsync("test.event", new { Value = 1 }, CancellationToken.None);
    }

    [Fact]
    public async Task PublishAsync_WhenHubFails_DoesNotThrow()
    {
        var httpHandler = new TestHttpHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var client = new HttpClient(httpHandler) { BaseAddress = new Uri("http://localhost:5005") };
        var logger = Substitute.For<ILogger<HttpEventPublisher>>();
        var publisher = new HttpEventPublisher(client, logger);

        await publisher.Invoking(p => p.PublishAsync("test.event", new { }, CancellationToken.None))
            .Should().NotThrowAsync();
    }
}

public class TestHttpHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;
    public TestHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) => _handler = handler;
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        => Task.FromResult(_handler(request));
}
