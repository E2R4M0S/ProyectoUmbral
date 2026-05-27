using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Teams.Application.Common.Interfaces;
using Teams.Infrastructure.Persistence;
using Teams.Infrastructure.Services;

namespace Teams.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<TeamsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Repositories
        services.AddScoped<IParticipantRepository, ParticipantRepository>();

        // Keycloak admin service
        services.AddHttpClient<IKeycloakAdminService, KeycloakAdminService>((sp, client) =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<KeycloakAdminOptions>>();
            client.BaseAddress = new Uri(options.Value.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Configuration binding
        services.Configure<KeycloakAdminOptions>(
            configuration.GetSection("KeycloakAdmin"));

        return services;
    }
}
