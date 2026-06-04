using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sessions.Application.Common.Interfaces;
using Sessions.Infrastructure.Notifications;
using Sessions.Infrastructure.Persistence;
using Sessions.Infrastructure.Services;

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

        // HTTP client to query Missions.Service for clue data
        services.AddHttpClient("MissionsClient", client =>
        {
            client.BaseAddress = new Uri(configuration["Missions:Url"] ?? "http://missions.service:80");
        });

        // Register a simple event publisher (RabbitMQ implementation placeholder)
        services.AddSingleton<IEventPublisher, Notifications.RabbitMqEventPublisher>();

        // Facade Pattern: coordina operaciones de sesion multi-paso
        services.AddScoped<IGameSessionFacade, GameSessionFacade>();

        return services;
    }
}
