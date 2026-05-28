using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Missions.Application.Common.Behaviors;
using Missions.Application.Common.Interfaces;
using Missions.Application.Common.Services;

namespace Missions.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddTransient(
            typeof(IPipelineBehavior<,>),
            typeof(ValidationBehavior<,>));

        services.AddScoped<IMissionLockService, PassthroughMissionLockService>();
        services.AddScoped<IMissionStageValidator, PassthroughMissionStageValidator>();

        return services;
    }
}
