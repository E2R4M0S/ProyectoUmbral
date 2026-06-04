using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Trivia.Infrastructure;
using Xunit;

namespace Trivia.Api.Tests.Endpoints;

public class AuthorizedEndpointsTests
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
                    options.UseInMemoryDatabase("AuthTest"));
            });
        });
    }

    [Fact]
    public async Task PostGameEnd_WithoutAuth_Returns401()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        var response = await client.PostAsync($"/games/{System.Guid.NewGuid()}/end", null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostGameEnd_WithOperatorRole_Returns200or400()
    {
        await using var factory = CreateFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", opts => { });
                services.AddScoped<Microsoft.AspNetCore.Authentication.IClaimsTransformation>(_ =>
                    new TestClaimsTransformer("operator"));
            });
        });
        var client = factory.CreateClient();
        var response = await client.PostAsync($"/games/{System.Guid.NewGuid()}/end", null);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetHealth_AlwaysPublic()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        var response = await client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
