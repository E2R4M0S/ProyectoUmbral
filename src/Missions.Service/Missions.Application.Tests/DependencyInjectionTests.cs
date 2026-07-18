using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Missions.Application.Common.Interfaces;
using Xunit;

namespace Missions.Application.Tests;

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
    public void AddApplicationServices_ShouldRegisterMissionLockService()
    {
        var services = new ServiceCollection();

        services.AddApplicationServices();

        services.Should().Contain(d => d.ServiceType == typeof(IMissionLockService));
    }

    [Fact]
    public void AddApplicationServices_ShouldRegisterMissionStageValidator()
    {
        var services = new ServiceCollection();

        services.AddApplicationServices();

        services.Should().Contain(d => d.ServiceType == typeof(IMissionStageValidator));
    }

    [Fact]
    public void AddApplicationServices_ShouldReturnSameServiceCollectionInstance()
    {
        var services = new ServiceCollection();

        var result = services.AddApplicationServices();

        result.Should().BeSameAs(services);
    }
}
