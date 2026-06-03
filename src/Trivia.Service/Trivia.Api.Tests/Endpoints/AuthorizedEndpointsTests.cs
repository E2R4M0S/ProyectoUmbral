using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Trivia.Api.Tests.Endpoints;

[Collection("TriviaApi")]
public class AuthorizedEndpointsTests
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthorizedEndpointsTests(TriviaApiFixture fixture) => _factory = fixture.Factory;

    [Fact]
    public async Task PostGameEnd_WithoutAuth_Returns401()
    {
        // Use a separate factory for this test - the default one doesn't mock auth
        var client = _factory.CreateClient();
        var response = await client.PostAsync($"/games/{System.Guid.NewGuid()}/end", null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostGameEnd_WithOperatorRole_Returns200or400()
    {
        var factory = _factory.WithWebHostBuilder(builder =>
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
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
