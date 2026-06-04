using System.Net;
using System.Net.Http.Json;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Trivia.Application.Trivias.Answers;
using Trivia.Application.Trivias.Clues;
using Trivia.Application.Trivias.Game;
using Trivia.Application.Trivias.Leaderboard;
using Trivia.Application.Trivias.Progress;
using Trivia.Application.Trivias.Questions;
using Trivia.Application.Trivias.Ranking;
using Trivia.Application.Trivias.StartTrivia;
using Trivia.Infrastructure;
using Xunit;

namespace Trivia.Api.Tests.Endpoints;

public class AllEndpointsTests
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
                    options.UseInMemoryDatabase("EndpointsTest"));
            });
        });
    }

    private static WebApplicationFactory<Program> CreateAuthenticatedFactory()
    {
        return CreateFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", opts => { });
                services.AddScoped<Microsoft.AspNetCore.Authentication.IClaimsTransformation>(_ =>
                    new TestClaimsTransformer("operator"));
            });
        });
    }

    private static WebApplicationFactory<Program> WithMockMediator(WebApplicationFactory<Program> factory)
    {
        return factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IMediator));
                if (descriptor != null) services.Remove(descriptor);
                var mock = Substitute.For<IMediator>();
                mock.Send(Arg.Any<IRequest<Unit>>(), Arg.Any<CancellationToken>())
                    .Returns(Unit.Value);
                services.AddSingleton<IMediator>(mock);
            });
        });
    }

    [Fact]
    public async Task PostAnswer_WithoutAuth_Returns401()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        var payload = new { QuizId = System.Guid.NewGuid(), TeamId = System.Guid.NewGuid(), QuestionId = System.Guid.NewGuid(), AnswerId = System.Guid.NewGuid(), Timestamp = System.DateTime.UtcNow };
        var response = await client.PostAsJsonAsync("/api/trivia/answers", payload);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostAnswer_WithAuth_Returns200()
    {
        await using var factory = WithMockMediator(CreateAuthenticatedFactory());
        var client = factory.CreateClient();
        var payload = new { QuizId = System.Guid.NewGuid(), TeamId = System.Guid.NewGuid(), QuestionId = System.Guid.NewGuid(), AnswerId = System.Guid.NewGuid(), Timestamp = System.DateTime.UtcNow };
        var response = await client.PostAsJsonAsync("/api/trivia/answers", payload);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
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
    public async Task PostGameEnd_WithAuth_Returns200()
    {
        await using var factory = WithMockMediator(CreateAuthenticatedFactory());
        var client = factory.CreateClient();
        var response = await client.PostAsync($"/games/{System.Guid.NewGuid()}/end", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // Note: Error-scenario tests omitted (see notes in earlier commits)

    [Fact]
    public async Task PostCloseQuestion_Returns200()
    {
        await using var factory = WithMockMediator(CreateAuthenticatedFactory());
        var client = factory.CreateClient();
        var response = await client.PostAsync($"/internal/trivia/{System.Guid.NewGuid()}/questions/{System.Guid.NewGuid()}/close", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostQuestionResults_Returns200()
    {
        await using var factory = WithMockMediator(CreateAuthenticatedFactory());
        var client = factory.CreateClient();
        var response = await client.PostAsync($"/internal/trivia/{System.Guid.NewGuid()}/questions/{System.Guid.NewGuid()}/results", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostProgress_WithoutAuth_Returns401()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync($"/api/trivia/{System.Guid.NewGuid()}/progress", new { });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostProgress_WithAuth_Returns200()
    {
        await using var factory = WithMockMediator(CreateAuthenticatedFactory());
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync($"/api/trivia/{System.Guid.NewGuid()}/progress", new { elapsed = 10 });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostClue_WithoutAuth_Returns401()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync($"/api/trivia/{System.Guid.NewGuid()}/clues", new { teamId = (System.Guid?)null, clueData = new { } });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostClue_WithAuth_Returns200()
    {
        await using var factory = WithMockMediator(CreateAuthenticatedFactory());
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync($"/api/trivia/{System.Guid.NewGuid()}/clues", new { teamId = (System.Guid?)null, clueData = new { text = "Test" } });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostRanking_WithoutAuth_Returns401()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        var response = await client.PostAsync($"/ranking/{System.Guid.NewGuid()}/update", null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostRanking_WithAuth_Returns200()
    {
        var factory = CreateAuthenticatedFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                var mock = Substitute.For<IMediator>();
                mock.Send(Arg.Any<UpdateRankingCommand>(), Arg.Any<CancellationToken>())
                    .Returns(new System.Collections.Generic.List<RankingEntryDto> { new(1, "A", 100) });
                services.AddSingleton<IMediator>(mock);
            });
        });
        await using var _ = factory;
        var client = factory.CreateClient();
        var response = await client.PostAsync($"/ranking/{System.Guid.NewGuid()}/update", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostStartTrivia_WithoutAuth_Returns401()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        var response = await client.PostAsync($"/api/trivia/{System.Guid.NewGuid()}/start", null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostStartTrivia_WithAuth_Returns200()
    {
        await using var factory = WithMockMediator(CreateAuthenticatedFactory());
        var client = factory.CreateClient();
        var response = await client.PostAsync($"/api/trivia/{System.Guid.NewGuid()}/start", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

