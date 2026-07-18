using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Sessions.Application.Sessions.Timeout;
using Xunit;

namespace Sessions.Application.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddApplicationServices_ShouldRegisterMediator()
    {
        var services = new ServiceCollection();
        services.AddLogging();

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
    public void AddApplicationServices_ShouldRegisterSessionTimeoutEnforcementService()
    {
        var services = new ServiceCollection();

        services.AddApplicationServices();

        services.Should().Contain(d =>
            d.ServiceType == typeof(SessionTimeoutEnforcementService) &&
            d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddApplicationServices_ShouldReturnSameServiceCollectionInstance()
    {
        var services = new ServiceCollection();

        var result = services.AddApplicationServices();

        result.Should().BeSameAs(services);
    }
}
