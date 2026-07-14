using Missions.Application.Common.Proxy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Missions.Application.Common.Interfaces;
using Missions.Infrastructure.Persistence;
using Missions.Infrastructure.Services;

namespace Missions.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<MissionsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<MissionRepository>();
        services.AddScoped<IMissionRepository>(sp =>
            new MissionAccessProxy(sp.GetRequiredService<MissionRepository>()));

        services.AddScoped<IParticipantRepository, ParticipantRepository>();

        services.AddHttpClient<IKeycloakAdminService, KeycloakAdminService>((sp, client) =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<KeycloakAdminOptions>>();
            client.BaseAddress = new Uri(options.Value.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.Configure<KeycloakAdminOptions>(
            configuration.GetSection("KeycloakAdmin"));

        return services;
    }
}
