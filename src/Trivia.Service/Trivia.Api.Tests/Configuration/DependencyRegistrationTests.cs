using System;
using System.Linq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Trivia.Application.Common.Interfaces;
using Trivia.Infrastructure;
using Trivia.Infrastructure.Persistence;
using Xunit;

namespace Trivia.Api.Tests.Configuration;

public class DependencyRegistrationTests
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
                    options.UseInMemoryDatabase("DITest"));
            });
        });
    }

    [Fact]
    public void AllApplicationServices_AreRegistered()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient(); // triggers host build

        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;

        sp.GetService<IQuizRepository>().Should().NotBeNull();
        sp.GetService<IAnswerRepository>().Should().NotBeNull();
        sp.GetService<IParticipantAnswerRepository>().Should().NotBeNull();
        sp.GetService<ILeaderboardRepository>().Should().NotBeNull();
        sp.GetService<IEventPublisher>().Should().NotBeNull();
    }

    [Fact]
    public void Repositories_AreScoped()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();

        using var scope1 = factory.Services.CreateScope();
        using var scope2 = factory.Services.CreateScope();

        var repo1 = scope1.ServiceProvider.GetService<IQuizRepository>();
        var repo2 = scope2.ServiceProvider.GetService<IQuizRepository>();

        repo1.Should().NotBeSameAs(repo2);
    }

    [Fact]
    public void DbContext_Resolves()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();

        using var scope = factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetService<TriviaDbContext>();
        ctx.Should().NotBeNull();
    }
}
