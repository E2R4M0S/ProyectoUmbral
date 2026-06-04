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

public class HealthEndpointTests
{
    private static WebApplicationFactory<Program> CreateFactory()
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Test");
            builder.ConfigureTestServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<TriviaDbContext>));
                if (descriptor != null) services.Remove(descriptor);
                services.AddDbContext<TriviaDbContext>(options =>
                    options.UseInMemoryDatabase("HealthTest"));
            });
        });
    }

    [Fact]
    public async Task GetHealth_Returns200()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        var response = await client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetHealth_ReturnsJson()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        var response = await client.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Healthy");
        body.Should().Contain("Trivia.Api");
    }

    [Fact]
    public async Task GetHealth_AllowAnonymous()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        var response = await client.GetAsync("/health");
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}
