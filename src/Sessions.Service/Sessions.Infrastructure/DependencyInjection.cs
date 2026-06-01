using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sessions.Application.Common.Interfaces;
using Sessions.Infrastructure.Notifications;
using Sessions.Infrastructure.Persistence;

namespace Sessions.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<SessionsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<ISessionRepository, SessionRepository>();

        // SignalR notification via HttpClient to RealTimeHub
        services.AddHttpClient<IGameNotifier, GameNotifier>(client =>
        {
            client.BaseAddress = new Uri(configuration["RealTimeHub:Url"] ?? "http://localhost:5005");
        });

        services.AddHttpClient<IEventPublisher, Sessions.Infrastructure.Messaging.HttpEventPublisher>(client =>
        {
            client.BaseAddress = new Uri(configuration["RealTimeHub:Url"] ?? "http://localhost:5005");
        });

        return services;
    }
}
