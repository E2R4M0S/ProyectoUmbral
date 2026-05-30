using Missions.Application.Common.Proxy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Missions.Application.Common.Interfaces;
using Missions.Infrastructure.Persistence;

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

        return services;
    }
}