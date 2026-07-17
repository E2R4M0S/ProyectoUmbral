using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Sessions.Application.Common.Behaviors;
using Sessions.Application.Sessions.Timeout;

namespace Sessions.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.AddScoped<SessionTimeoutEnforcementService>();

        return services;
    }
}
