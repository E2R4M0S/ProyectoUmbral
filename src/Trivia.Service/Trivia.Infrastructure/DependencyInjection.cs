using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Trivia.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Use Postgres provider per project skills
        services.AddDbContext<TriviaDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Repositories
        services.AddScoped<Trivia.Application.Common.Interfaces.IQuizRepository, Trivia.Infrastructure.Persistence.QuizRepository>();
        services.AddScoped<Trivia.Application.Common.Interfaces.IParticipantAnswerRepository, Trivia.Infrastructure.Persistence.ParticipantAnswerRepository>();
        services.AddScoped<Trivia.Application.Common.Interfaces.IAnswerRepository, Trivia.Infrastructure.Persistence.AnswerRepository>();
        services.AddScoped<Trivia.Application.Common.Interfaces.ILeaderboardRepository, Trivia.Infrastructure.Persistence.LeaderboardRepository>();

        // Http-based event publisher to RealTimeHub (keeps microservice decoupling)
        // Prefer RabbitMQ publisher when RABBITMQ_HOST is present, otherwise fallback to HTTP bridge
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RABBITMQ_HOST")))
        {
            services.AddSingleton<Trivia.Application.Common.Interfaces.IEventPublisher, Trivia.Infrastructure.Messaging.RabbitMQ.RabbitMqEventPublisher>();
        }
        else
        {
            services.AddHttpClient<Trivia.Application.Common.Interfaces.IEventPublisher, Trivia.Infrastructure.Messaging.HttpEventPublisher>(client =>
            {
                client.BaseAddress = new Uri(configuration["RealTimeHub:Url"] ?? "http://localhost:5005");
            });
        }

        // RabbitMQ consumer for leaderboard updates
        services.AddHostedService<Trivia.Infrastructure.Messaging.RabbitMQ.TriviaAnswerSubmittedConsumer>();

        return services;
    }
}
