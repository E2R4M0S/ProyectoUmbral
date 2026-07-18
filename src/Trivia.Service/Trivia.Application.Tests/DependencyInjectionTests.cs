using System.Net.Http;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Trivia.Application;
using Xunit;

namespace Trivia.Application.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddApplicationServices_ShouldRegisterMediator()
    {
        var services = new ServiceCollection();

        services.AddApplicationServices();
        var provider = services.BuildServiceProvider();

        provider.GetService<IMediator>().Should().NotBeNull();
    }

    [Fact]
    public void AddApplicationServices_ShouldRegisterValidationPipelineBehavior()
    {
        var services = new ServiceCollection();

        services.AddApplicationServices();

        services.Should().Contain(d => d.ServiceType == typeof(IPipelineBehavior<,>));
    }

    [Fact]
    public void AddApplicationServices_ShouldRegisterRealTimeHubHttpClientWithExpectedBaseAddress()
    {
        var services = new ServiceCollection();

        services.AddApplicationServices();
        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient("RealTimeHub");

        client.BaseAddress.Should().Be(new System.Uri("http://localhost:5005"));
    }

    [Fact]
    public void AddApplicationServices_ShouldReturnSameServiceCollectionInstance()
    {
        var services = new ServiceCollection();

        var result = services.AddApplicationServices();

        result.Should().BeSameAs(services);
    }
}
