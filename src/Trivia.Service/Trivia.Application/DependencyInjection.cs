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

        // Http client used to call the RealTimeHub internal endpoints
        services.AddHttpClient("RealTimeHub", client =>
        {
            client.BaseAddress = new Uri("http://localhost:5005");
        });

        return services;
    }
}
