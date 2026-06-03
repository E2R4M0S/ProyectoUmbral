using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Trivia.Application.Tests;

public class TestHttpMessageHandler : HttpMessageHandler
{
    private readonly System.Func<HttpRequestMessage, HttpResponseMessage> _handler;

    public TestHttpMessageHandler(System.Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        _handler = handler;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_handler(request));
    }
}
