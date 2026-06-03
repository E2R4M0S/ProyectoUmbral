using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Trivia.Infrastructure;
using Xunit;

namespace Trivia.Api.Tests.Endpoints;

[Collection("TriviaApi")]
public class HealthEndpointTests
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthEndpointTests(TriviaApiFixture fixture) => _factory = fixture.Factory;

    [Fact]
    public async Task GetHealth_Returns200()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetHealth_ReturnsJson()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Healthy");
        body.Should().Contain("Trivia.Api");
    }

    [Fact]
    public async Task GetHealth_AllowAnonymous()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health");
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}
