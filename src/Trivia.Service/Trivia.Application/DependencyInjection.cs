using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Trivia.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // register application handlers in Trivia.Application
        // (MediatR registration above will pick up handlers in this assembly)

        return services;
    }
}
